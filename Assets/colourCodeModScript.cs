using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

using Random = UnityEngine.Random;

public class colourCodeModScript : MonoBehaviour {
    public KMAudio BombAudio;
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMSelectable[] NumberedButtons;
    public KMSelectable[] ColouredButtons;
    public KMSelectable submitButton;
    public KMSelectable deleteButton;
    public KMSelectable ModuleSelect;
    public GameObject[] screenPieces;

    private String[] answerText;

    int solvedModules;

    bool moduleSolved;

    static int moduleIdCounter = 1;
    int moduleId;

    void Start() {
        moduleId = moduleIdCounter++;

        CalculateCorrectAnswer();

        for (int i = 0; i < NumberedButtons.Length; i++) {
            int j = i;

            NumberedButtons[i].OnInteract += delegate() {
                PressNumberedButton(j);

                return false;
            };
        }

        for (int i = 0; i < ColouredButtons.Length; i++) {
            int j = i;

            ColouredButtons[i].OnInteract += delegate() {
                PressColouredButton(j);

                return false;
            };
        }

        submitButton.OnInteract +=delegate(){
            PressSubmitButton();
            return false;
        }
        deleteButton.OnInteract +=delegate(){
            PressDeleteButton();
            return false;
        }
    }

    void Update() {
        if (!moduleSolved) {
            var newSolvedModules = BombInfo.GetSolvedModuleNames().Count;

            if (newSolvedModules != solvedModules) {
                solvedModules = newSolvedModules;
                CalculateCorrectAnswer();
            }
        }
    }

    int getTotalModuleCountByName(String n) {
        return BombInfo.GetSolvableModuleNames().Count( x => x == n);
    }
    int getSolvedModuleCountByName(String n) {
        return BombInfo.GetSolvedModuleNames().Count( x => x == n);
    }
    int getUnsolvedModuleCountByName(String n) {
        return getTotalModuleCountByName(n)-getSolvedModuleCountByName(n);
    }

    void doLog(String m) {
        Debug.LogFormat("[Colour Code #{0}] {1}",moduleId,m);
    }

    void CalculateCorrectAnswer() {
        int firstDigit=0;
        if(BombInfo.GetBatteryCount()<=1) {
            firstDigit=3;
        } else if(BombInfo.IsIndicatorOn("FRK")) {
            firstDigit=6;
        } else if(BombInfo.GetPortCount()>BombInfo.GetBatteryCount()) {
            firstDigit=7;
        } else if(BombInfo.GetOnIndicators()>BombInfo.GetSolvedModuleNames().Count()) {
            firstDigit=9;
        } else if((BombInfo.GetOnIndicators()+BombInfo.GetOffIndicators()+BombInfo.GetPortCount())<getTotalModuleCountByName("Colour Code")) {
            firstDigit=2;
        } else if(BombInfo.GetBatteryCount()<getTotalModuleCountByName("Planets")) {
            firstDigit=5;
        } else if((BombInfo.GetSolvableModuleNames().Count()-BombInfo.GetSolvedModuleNames().Count())>40) {
            firstDigit=8;
        } else if(BombInfo.GetBatteryCount(Battery.AA)==2&&BombInfo.GetBatteryCount(Battery.D)==2) {
            firstDigit=1;
        } else if((BombInfo.GetSolvableModuleNames().Count()/2)<BombInfo.GetSolvedModuleNames().Count()) {
            firstDigit=4;
        }


        int secondDigit=0;
        if(backgroundColour=="red") {
            if(BombInfo.GetPortCount(Port.Parallel)>0) {
                secondDigit=5;
            } else {
                secondDigit=3;
            }
        } else if(backgroundColour=="orange") {
            if(BombInfo.GetBatteryCount()>(BombInfo.GetIndicators()-BombInfo.GetPortCount())) {
                secondDigit=9;
            } else {
                secondDigit=4;
            }
        } else if(backgroundColour=="green") {
            if(BombInfo.GetOnIndicators()>getTotalModuleCountByName("Planets")) {
                secondDigit=8;
            } else {
                secondDigit=1;
            }
        } else if(backgroundColour=="yellow") {
            if((getUnsolvedModuleCountByName("Colour Code")+getUnsolvedModuleCountByName("Planets"))>(getSolvedModuleCountByName("Colour Code")+getSolvedModuleCountByName("Planets"))) {
                secondDigit=7;
            } else {
                secondDigit=2;
            }
        } else if(backgroundColour=="blue") {
            if((BombInfo.GetSolvableModuleNames().Count()-BombInfo.GetSolvedModuleNames().Count())==1) {
                secondDigit=6;
            }
        }


        
    }

    void PressButton(int buttonId) {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch();

        if (moduleSolved) {
            return;
        }

        if (buttonId == 11) {
            myText = myText.PadLeft(6, '0');

            if (!myText.Equals("") && answerText == myText) {
                moduleSolved = true;
                myText = "Solved";
                RenderScreen();
                doLog("Module solved.");
                BombModule.HandlePass();
                StartCoroutine(EndAnimation());
            } else {
                doLog("Strike! The PIN "+myText+" is incorrect.");
                myText = "";
                BombModule.HandleStrike();
            }
        } else if (buttonId == 10) {
            if (myText.Length > 0) {
                myText = myText.Remove(myText.Length - 1);
            }
        } else {
            myText += buttonId;
        }

        RenderScreen();
    }

    void RenderScreen() {
        if (myText.Length > 6 && myText != "Help Me" && myText != "Solved") {
            myText = myText.Remove(myText.Length - 1);
        }

        screenText.text = myText;

        if (myText.Length == 0) {
            screenText.text = "Help Me";
        }
    }

    IEnumerator EndAnimation() {
        for (int i = 0; i < 100; i++) {
            planetModels[planetShown].transform.Translate(0.0f, -0.0005f, 0.0f);
            yield return null;
        }
        for (int i=0;i<stripLights.Length;i++) {
            for (int j = 0; j < stripLights[i].transform.childCount; j++) {
                stripLights[i].transform.GetChild(j).gameObject.SetActive((stripLights[i].transform.childCount-1 == j));
                yield return null;
            }
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Submit your answer with “!{0} press 1234 delete space”.";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command) {
        command = command.ToLowerInvariant().Trim();

        if (Regex.IsMatch(command, @"^press +[0-9a-z^, |&]+$")) {
            command = command.Substring(6).Trim();
            var presses = command.Split(new[] { ',', ' ', '|', '&' }, StringSplitOptions.RemoveEmptyEntries);
            var pressList = new List<KMSelectable>();

            for (int i = 0; i < presses.Length; i++) {
                if (Regex.IsMatch(presses[i], @"^(delete|space)$")) {
                    pressList.Add(ModuleButtons[(presses[i].Equals("delete")) ? 10 : 11]);
                } else {
                    string numpadPresses = presses[i];

                    for (int j = 0; j < numpadPresses.Length; j++) {
                        if (Regex.IsMatch(numpadPresses[j].ToString(), @"^[0-9]$")) {
                            pressList.Add(ModuleButtons[int.Parse(numpadPresses[j].ToString())]);
                        }
                    }
                }
            }

            return (pressList.Count > 0) ? pressList.ToArray() : null;
        }

        return null;
    }
}
