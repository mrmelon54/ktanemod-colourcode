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
    public Material[] materials;

    private List<String> allTheDigits=new List<String>();
    private String myText="";
    private List<object> screenText=new List<object>();
    private String answerText;
    private List<String> finalText=new List<String>();
    private List<String> answerOrder=new List<String>();
    private String backgroundColour;

    int solvedModules;

    bool moduleSolved;

    static int moduleIdCounter = 1;
    int moduleId;

    void Start() {
        moduleId = moduleIdCounter++;

        CalculateCorrectAnswer();
        CalculateDigitOrder();
        PrepareCorrectAnswer();

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

        submitButton.OnInteract += delegate() {
            PressSubmitButton();
            return false;
        };
        deleteButton.OnInteract += delegate() {
            PressDeleteButton();
            return false;
        };
    }

    int LastModulesSolved=0;

    void Update() {
        if (!moduleSolved&&LastModulesSolved!=BombInfo.GetSolvedModuleNames().Count()) {
            CalculateCorrectAnswer();
            CalculateDigitOrder();
            PrepareCorrectAnswer();
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
        } else if(BombInfo.GetOnIndicators().Count()>BombInfo.GetSolvedModuleNames().Count()) {
            firstDigit=9;
        } else if((BombInfo.GetOnIndicators().Count()+BombInfo.GetOffIndicators().Count()+BombInfo.GetPortCount())<getTotalModuleCountByName("Colour Code")) {
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
            if(BombInfo.GetBatteryCount()>(BombInfo.GetIndicators().Count()-BombInfo.GetPortCount())) {
                secondDigit=9;
            } else {
                secondDigit=4;
            }
        } else if(backgroundColour=="green") {
            if(BombInfo.GetOnIndicators().Count()>getTotalModuleCountByName("Planets")) {
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


        int thirdDigit=0;
        int bit1=(BombInfo.GetBatteryCount()+2)*BombInfo.GetSolvedModuleNames().Count();
        int bit2=bit1-(BombInfo.GetOnIndicators().Count()>BombInfo.GetOffIndicators().Count()?15:0);
        int bit3=bit2+(backgroundColour=="red"?150:0);
        int bit4=bit3/(bit3%3==0?3:1);
        int bit5=bit4%10;
        int bit6=bit5*(BombInfo.GetSerialNumber().First().Equals("0")?2:1);
        int bit7=bit6*(BombInfo.GetSerialNumber()[1].Equals("0")?4:1);
        int bit8=bit7%10;
        thirdDigit=Math.Abs(bit8);
        // only allow if the last seconds digit is the code digit



        int fourthDigit=0;
        int bob1=100-(firstDigit+secondDigit+thirdDigit);
        int bob2=bob1-(getTotalModuleCountByName("Colour Code")+(BombInfo.GetSolvableModuleNames().Count()-BombInfo.GetSolvedModuleNames().Count()));
        int bob3=bob2+BombInfo.GetOffIndicators().Count();
        int bob4=bob3%10;
        fourthDigit=bob4;



        String firstColour="purple"; // default to purple
        if(backgroundColour=="red" && BombInfo.GetPortCount()==0 && BombInfo.GetIndicators().Count()==0 && BombInfo.GetSolvedModuleNames().Count()==0) {
            firstColour="red";
        } else if(backgroundColour=="orange" && BombInfo.GetSerialNumberNumbers().Sum()%10==BombInfo.GetBatteryCount()) {
            firstColour="orange";
        } else if(backgroundColour=="green" && BombInfo.GetPortCount()>BombInfo.GetOffIndicators().Count()) {
            firstColour="green";
        } else if(backgroundColour=="yellow" && BombInfo.GetOffIndicators().Count()==1 && BombInfo.GetSerialNumberNumbers().Last()%2==1) {
            firstColour="yellow";
        } else if(backgroundColour=="blue" && BombInfo.GetBatteryCount()==BombInfo.GetSolvedModuleNames().Count()) {
            firstColour="blue";
        }



        String secondColour="purple"; // default to purple
        if(BombInfo.GetSerialNumberNumbers().Last()%2==0) {
            secondColour="blue";
        } else if(BombInfo.GetPortCount(Port.Parallel)>0) {
            secondColour="green";
        } else if((BombInfo.GetBatteryCount()+BombInfo.GetSerialNumberNumbers().Sum())<=5) {
            secondColour="orange";
        } else if(BombInfo.GetBatteryCount(Battery.AA)==BombInfo.GetBatteryCount()) {
            secondColour="red";
        } else if(backgroundColour=="yellow") {
            secondColour="yellow";
        }



        String thirdColour="purple";
        int bar1=(BombInfo.GetSolvableModuleNames().Count()-BombInfo.GetSolvedModuleNames().Count())*BombInfo.GetSolvedModuleNames().Count();
        int bar2=bar1/(bar1%3==0?3:1);
        int bar3=bar2%10;
        int bar4=bar3*(firstColour=="purple"?2:1);
        int bar5=bar4*(secondColour=="purple"?4:1);
        int bar6=bar5%10;
        int bar7=bar6*BombInfo.GetBatteryCount();
        int bar8=bar7%6; // I removed the +1 so 0=orange instead of 1=orange
        String[] colourNumberConvertor={"orange","blue","red","purple","yellow","green"};
        thirdColour=colourNumberConvertor[bar8];



        allTheDigits.Clear();
        allTheDigits.Add(firstDigit.ToString());
        allTheDigits.Add(secondDigit.ToString());
        allTheDigits.Add(thirdDigit.ToString());
        allTheDigits.Add(fourthDigit.ToString());
        allTheDigits.Add(firstColour);
        allTheDigits.Add(secondColour);
        allTheDigits.Add(thirdColour);
        doLog("Digit 1 = "+firstDigit.ToString());
        doLog("Digit 2 = "+secondDigit.ToString());
        doLog("Digit 3 = "+thirdDigit.ToString());
        doLog("Digit 4 = "+fourthDigit.ToString());
        doLog("Colour 1 = "+firstColour);
        doLog("Colour 2 = "+secondColour);
        doLog("Colour 3 = "+thirdColour);
    }

    void CalculateDigitOrder() {
        int productOfDigits=(int.Parse(allTheDigits[0])*int.Parse(allTheDigits[1])*int.Parse(allTheDigits[2])*int.Parse(allTheDigits[3]));
        bool con1=BombInfo.GetBatteryCount()>12;
        bool con2=productOfDigits>BombInfo.GetModuleNames().Count();
        bool con3=BombInfo.GetModuleNames().Count()==1;
        bool con4=DateTime.Now.Hour>=3&&DateTime.Now.Hour<4;
        bool con5=BombInfo.GetModuleNames().Count()==101;
        bool con6=getTotalModuleCountByName("Colour Code")>Math.Sqrt(BombInfo.GetModuleNames().Count());
        bool con7=BombInfo.GetSerialNumberLetters().Count()==3;

        answerOrder.Clear();
        if(con1) answerOrder.Add("digit");
        if(con2) answerOrder.Add("colour");
        if(con3) answerOrder.Add("digit");
        if(con4) answerOrder.Add("colour");
        if(con5) answerOrder.Add("digit");
        if(con6) answerOrder.Add("colour");
        if(con7) answerOrder.Add("digit");

        if(!con7) answerOrder.Add("digit");
        if(!con6) answerOrder.Add("colour");
        if(!con5) answerOrder.Add("digit");
        if(!con4) answerOrder.Add("colour");
        if(!con3) answerOrder.Add("digit");
        if(!con2) answerOrder.Add("colour");
        if(!con1) answerOrder.Add("digit");

        doLog("Order of digits conditions = "+con1.ToString()+" "+con2.ToString()+" "+con3.ToString()+" "+con4.ToString()+" "+con5.ToString()+" "+con6.ToString()+" "+con7.ToString());
    }

    void PrepareCorrectAnswer() {
        finalText.Clear();

        int _dp=1;
        int _cp=1;

        String _logText="Order of digits = ";

        for(int i=0;i<answerOrder.Count;i++) {
            if(answerOrder[i]=="digit") {
                finalText.Add(_dp==1?allTheDigits[0]:_dp==2?allTheDigits[1]:_dp==3?allTheDigits[2]:allTheDigits[3]);
                if(i!=answerOrder.Count()) _logText+="d"+_dp.ToString()+", ";
                _dp++;
            } else {
                finalText.Add(_cp==1?allTheDigits[4]:_cp==2?allTheDigits[5]:allTheDigits[6]);
                if(i!=answerOrder.Count()) _logText+="c"+_cp.ToString()+", ";
                _cp++;
            }
        }

        doLog(_logText);

        answerText="";
        for(int i=0;i<finalText.Count;i++) {
            String c=finalText[i];
            answerText+=c=="orange"?"o":c=="blue"?"b":c=="red"?"r":c=="purple"?"p":c=="yellow"?"y":c=="green"?"g":c;
        }

        doLog("Correct answer is: "+answerText);
    }

    void PressNumberedButton(int buttonId) {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch();

        if (moduleSolved) {
            return;
        }

        String buttonText=NumberedButtons[buttonId].gameObject.name.Replace("Button","");

        String myTextSpliting="";
        for(int i=0;i<myText.Length;i++) {
            myTextSpliting+=myText[i]+".";
        }
        String[] myTextSplit=myTextSpliting.Split('.');

        if(ArrayCountAnArray(myTextSplit,"0.1.2.3.4.5.6.7.8.9".Split('.'))==2) {
            int TimerAsInteger=int.Parse(BombInfo.GetTime().ToString().Split('.')[0]);
            if(((TimerAsInteger%60)%10).ToString()==buttonText) {
                myText+=buttonText;
            } else {
                doLog("Press this digit when the last digit of the seconds is equal to it");
                BombModule.HandleStrike();
            }
        } else {
            myText+=buttonText;
        }


        PrepareRenderReadyText();
        RenderScreen();
    }

    void PressColouredButton(int buttonId) {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch();

        if (moduleSolved) {
            return;
        }

        myText+=ColouredButtons[buttonId].gameObject.name.Replace("Button","");

        PrepareRenderReadyText();
        RenderScreen();
    }

    void PressSubmitButton() {
        doLog("You entered: |"+myText+"|");
        doLog("Correct answer: |"+answerText+"|");
        if(String.Equals(myText,answerText)) {
            String myTextSpliting="";
            for(int i=0;i<myText.Length;i++) {
                myTextSpliting+=myText[i]+".";
            }
            String[] myTextSplit=myTextSpliting.Split('.');
            if(ArrayCount(myTextSplit,"0")==1&&ArrayCount(myTextSplit,"p")==1) {
                var seconds=(BombInfo.GetTime()%60).ToString();
                if(seconds=="40"||seconds=="04") {
                    moduleSolved=true;
                    BombModule.HandlePass();
                } else {
                    BombModule.HandleStrike();
                }
            } else {
                moduleSolved=true;
                BombModule.HandlePass();
            }
        } else {
            BombModule.HandleStrike();
        }
    }

    void PressDeleteButton() {
        if(myText.Length>0) {
            myText=myText.Remove(myText.Length-1,1);
            PrepareRenderReadyText();
            RenderScreen();
        }
    }



    int ArrayCount(String[] a,String b) {
        int o=0;
        for(int i=0;i<a.Length;i++) {
            if(String.Equals(a[i],b)) {
                o++;
            }
        }
        return o;
    }

    int ArrayCountAnArray(String[] a,String[] b) {
        int o=0;
        for(int i=0;i<a.Length;i++) {
            for(int j=0;j<b.Length;j++) {
                if(String.Equals(a[i],b[j])) {
                    o++;
                    break;
                }
            }
        }
        return o;
    }

    void PrepareRenderReadyText() {
        myText=myText.ToLower();
        String myTextSpliting="";
        for(int i=0;i<myText.Length;i++) {
            myTextSpliting+=myText[i]+".";
        }
        String[] myTextSplit=myTextSpliting.Split('.');
        screenText.Clear();
        for(int i=0;i<myTextSplit.Length;i++) {
            String c=myTextSplit[i];
            switch(c) {
                case "r":
                    screenText.Add(materials[0]);
                    break;
                case "o":
                    screenText.Add(materials[1]);
                    break;
                case "y":
                    screenText.Add(materials[2]);
                    break;
                case "g":
                    screenText.Add(materials[3]);
                    break;
                case "b":
                    screenText.Add(materials[4]);
                    break;
                case "p":
                    screenText.Add(materials[5]);
                    break;
                default:
                    screenText.Add(c);
                    break;
            }
        }
    }

    void RenderScreen() {
        for(int i=0;i<screenPieces.Length;i++) {
            RenderBlock(i," ");
        }
        if(moduleSolved) {
            screenText.Clear();
            screenText.Add("S");
            screenText.Add("o");
            screenText.Add("l");
            screenText.Add("v");
            screenText.Add("e");
            screenText.Add("d");
        } else if(myText.Length==0) {
            screenText.Clear();
            screenText.Add("H");
            screenText.Add("e");
            screenText.Add("l");
            screenText.Add("p");
            screenText.Add(" ");
            screenText.Add("M");
            screenText.Add("e");
        }
        for(int i=0;i<screenText.Count;i++) {
            if(screenText[i].GetType()==typeof(Material)) {
                RenderBlock(i,(Material)screenText[i]);
            } else if(screenText[i].GetType()==typeof(String)) {
                RenderBlock(i,(String)screenText[i]);
            } else {
                RenderBlock(i," ");
            }
        }
    }

    void RenderBlock(int i, String m) {
        if(i>=7) return;
        Transform pos=screenPieces[i].transform;
        pos.GetChild(0).gameObject.SetActive(true);
        pos.GetChild(1).gameObject.SetActive(false);
        pos.GetChild(0).gameObject.GetComponent<TextMesh>().text=m;
    }

    void RenderBlock(int i, Material m) {
        if(i>=7) return;
        Transform pos=screenPieces[i].transform;
        pos.GetChild(0).gameObject.SetActive(false);
        pos.GetChild(1).gameObject.SetActive(true);
        pos.GetChild(1).gameObject.GetComponent<Renderer>().material=m;
    }






    // For KingSlendy :)
    // Pls update this for this module
    // Make sure to add the press at a specific time bit

/*#pragma warning disable 414
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
    }*/
}
