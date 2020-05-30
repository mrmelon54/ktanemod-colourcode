using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

public class colourCodeModScript : MonoBehaviour
{
    public KMAudio BombAudio;
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMSelectable[] NumberedButtons;
    public KMSelectable[] ColouredButtons;
    public KMSelectable submitButton;
    public KMSelectable deleteButton;
    public KMSelectable ModuleSelect;
    public GameObject[] screenPieces;
    public GameObject moduleBackground;
    public Material[] materials;
    public Material[] materialsLight;
    public GameObject nothingText;

    private List<string> allTheDigits = new List<String>();
    private string myText = "";
    private List<object> screenText = new List<object>();
    private string answerText;
    private List<string> finalText = new List<String>();
    private List<string> answerOrder = new List<String>();
    private string backgroundColour;
    private bool cancelGoCommand = false;

    int solvedModules;

    bool moduleSolved;

    static int moduleIdCounter = 1;
    int moduleId;

    void Start()
    {
        moduleId = moduleIdCounter++;

        nothingText.SetActive(false);

        string[] backgroundTranslationTable = { "red", "orange", "yellow", "green", "blue", "purple" };
        int newBackground = Random.Range(0, materials.Length - 1);
        backgroundColour = backgroundTranslationTable[newBackground];
        moduleBackground.GetComponent<Renderer>().material = materials[newBackground];

        CalculateCorrectAnswer();
        CalculateDigitOrder();
        PrepareCorrectAnswer();

        for (int i = 0; i < NumberedButtons.Length; i++)
        {
            int j = i;

            NumberedButtons[i].OnInteract += delegate ()
            {
                PressNumberedButton(j);

                return false;
            };
        }

        for (int i = 0; i < ColouredButtons.Length; i++)
        {
            int j = i;

            ColouredButtons[i].OnInteract += delegate ()
            {
                PressColouredButton(j);

                return false;
            };
        }

        submitButton.OnInteract += delegate ()
        {
            PressSubmitButton();
            return false;
        };
        deleteButton.OnInteract += delegate ()
        {
            PressDeleteButton();
            return false;
        };
    }

    int LastModulesSolved = 0;

    void Update()
    {
        if (!moduleSolved && LastModulesSolved != BombInfo.GetSolvedModuleNames().Count())
        {
            CalculateCorrectAnswer();
            CalculateDigitOrder();
            PrepareCorrectAnswer();
            LastModulesSolved = BombInfo.GetSolvedModuleNames().Count();
        }
    }

    int getTotalModuleCountByName(string n)
    {
        return BombInfo.GetSolvableModuleNames().Count(x => x == n);
    }
    int getSolvedModuleCountByName(string n)
    {
        return BombInfo.GetSolvedModuleNames().Count(x => x == n);
    }
    int getUnsolvedModuleCountByName(string n)
    {
        return getTotalModuleCountByName(n) - getSolvedModuleCountByName(n);
    }

    void doLog(string m)
    {
        Debug.LogFormat("[Colour Code #{0}] {1}", moduleId, m);
    }

    void CalculateCorrectAnswer()
    {
        int firstDigit = 0;
        if (BombInfo.GetBatteryCount() <= 1)
        {
            firstDigit = 3;
        }
        else if (BombInfo.IsIndicatorOn("FRK"))
        {
            firstDigit = 6;
        }
        else if (BombInfo.GetPortCount() > BombInfo.GetBatteryCount())
        {
            firstDigit = 7;
        }
        else if (BombInfo.GetOnIndicators().Count() > BombInfo.GetSolvedModuleNames().Count())
        {
            firstDigit = 9;
        }
        else if ((BombInfo.GetOnIndicators().Count() + BombInfo.GetOffIndicators().Count() + BombInfo.GetPortCount()) < getTotalModuleCountByName("Colour Code"))
        {
            firstDigit = 2;
        }
        else if (BombInfo.GetBatteryCount() < getTotalModuleCountByName("Planets"))
        {
            firstDigit = 5;
        }
        else if ((BombInfo.GetSolvableModuleNames().Count() - BombInfo.GetSolvedModuleNames().Count()) > 40)
        {
            firstDigit = 8;
        }
        else if (BombInfo.GetBatteryCount(Battery.AA) == 2 && BombInfo.GetBatteryCount(Battery.D) == 2)
        {
            firstDigit = 1;
        }
        else if ((BombInfo.GetSolvableModuleNames().Count() / 2) < BombInfo.GetSolvedModuleNames().Count())
        {
            firstDigit = 4;
        }


        int secondDigit = 0;
        if (backgroundColour == "red")
        {
            if (BombInfo.GetPortCount(Port.Parallel) > 0)
            {
                secondDigit = 5;
            }
            else
            {
                secondDigit = 3;
            }
        }
        else if (backgroundColour == "orange")
        {
            if (BombInfo.GetBatteryCount() > (BombInfo.GetIndicators().Count() - BombInfo.GetPortCount()))
            {
                secondDigit = 9;
            }
            else
            {
                secondDigit = 4;
            }
        }
        else if (backgroundColour == "green")
        {
            if (BombInfo.GetOnIndicators().Count() > getTotalModuleCountByName("Planets"))
            {
                secondDigit = 8;
            }
            else
            {
                secondDigit = 1;
            }
        }
        else if (backgroundColour == "yellow")
        {
            if ((getUnsolvedModuleCountByName("Colour Code") + getUnsolvedModuleCountByName("Planets")) > (getSolvedModuleCountByName("Colour Code") + getSolvedModuleCountByName("Planets")))
            {
                secondDigit = 7;
            }
            else
            {
                secondDigit = 2;
            }
        }
        else if (backgroundColour == "blue")
        {
            if ((BombInfo.GetSolvableModuleNames().Count() - BombInfo.GetSolvedModuleNames().Count()) == 1)
            {
                secondDigit = 6;
            }
        }


        int thirdDigit = 0;
        int bit1 = (BombInfo.GetBatteryCount() + 2) * BombInfo.GetSolvedModuleNames().Count();
        int bit2 = bit1 - (BombInfo.GetOnIndicators().Count() > BombInfo.GetOffIndicators().Count() ? 15 : 0);
        int bit3 = bit2 + (backgroundColour == "red" ? 150 : 0);
        int bit4 = bit3 / (bit3 % 3 == 0 ? 3 : 1);
        int bit5 = bit4 % 10;
        int bit6 = bit5 * (firstDigit == 0 ? 2 : 1);
        int bit7 = bit6 * (secondDigit == 0 ? 4 : 1);
        int bit8 = bit7 % 10;
        thirdDigit = Math.Abs(bit8);
        // only allow if the last seconds digit is the code digit



        int fourthDigit = 0;
        int bob1 = 100 - (firstDigit + secondDigit + thirdDigit);
        int bob2 = bob1 - (getTotalModuleCountByName("Colour Code") + (BombInfo.GetSolvableModuleNames().Count() - BombInfo.GetSolvedModuleNames().Count()));
        int bob3 = bob2 + BombInfo.GetOffIndicators().Count();
        int bob4 = Math.Abs(bob3) % 10;
        fourthDigit = bob4;



        string firstColour = "purple"; // default to purple
        if (backgroundColour == "red" && BombInfo.GetPortCount() == 0 && BombInfo.GetIndicators().Count() == 0 && BombInfo.GetSolvedModuleNames().Count() == 0)
        {
            firstColour = "red";
        }
        else if (backgroundColour == "orange" && BombInfo.GetSerialNumberNumbers().Sum() % 10 == BombInfo.GetBatteryCount())
        {
            firstColour = "orange";
        }
        else if (backgroundColour == "green" && BombInfo.GetPortCount() > BombInfo.GetOffIndicators().Count())
        {
            firstColour = "green";
        }
        else if (backgroundColour == "yellow" && BombInfo.GetOffIndicators().Count() == 1 && BombInfo.GetSerialNumberNumbers().Last() % 2 == 1)
        {
            firstColour = "yellow";
        }
        else if (backgroundColour == "blue" && BombInfo.GetBatteryCount() == BombInfo.GetSolvedModuleNames().Count())
        {
            firstColour = "blue";
        }



        string secondColour = "purple"; // default to purple
        if (BombInfo.GetSerialNumberNumbers().Last() % 2 == 0)
        {
            secondColour = "blue";
        }
        else if (BombInfo.GetPortCount(Port.Parallel) > 0)
        {
            secondColour = "green";
        }
        else if (((BombInfo.GetBatteryCount() + BombInfo.GetSerialNumberNumbers().Sum()) % 10) <= 5)
        {
            secondColour = "orange";
        }
        else if (BombInfo.GetBatteryCount(Battery.D) == 0 && BombInfo.GetBatteryCount() > 0)
        {
            secondColour = "red";
        }
        else if (backgroundColour == "yellow")
        {
            secondColour = "yellow";
        }



        string thirdColour = "purple";
        int bar1 = (BombInfo.GetSolvableModuleNames().Count() - BombInfo.GetSolvedModuleNames().Count()) * BombInfo.GetSolvedModuleNames().Count();
        int bar2 = bar1 / (bar1 % 3 == 0 ? 3 : 1);
        int bar3 = bar2 % 10;
        int bar4 = bar3 * (firstColour == "purple" ? 2 : 1);
        int bar5 = bar4 * (secondColour == "purple" ? 4 : 1);
        int bar6 = bar5 % 10;
        int bar7 = bar6 * BombInfo.GetBatteryCount();
        int bar8 = bar7 % 6; // I removed the +1 so 0=orange instead of 1=orange
        string[] colourNumberConvertor = { "orange", "blue", "red", "purple", "yellow", "green" };
        thirdColour = colourNumberConvertor[bar8];



        allTheDigits.Clear();
        allTheDigits.Add(firstDigit.ToString());
        allTheDigits.Add(secondDigit.ToString());
        allTheDigits.Add(thirdDigit.ToString());
        allTheDigits.Add(fourthDigit.ToString());
        allTheDigits.Add(firstColour);
        allTheDigits.Add(secondColour);
        allTheDigits.Add(thirdColour);
        doLog("Digit 1 = " + firstDigit.ToString());
        doLog("Digit 2 = " + secondDigit.ToString());
        doLog("Digit 3 = " + thirdDigit.ToString());
        doLog("Digit 4 = " + fourthDigit.ToString());
        doLog("Colour 1 = " + firstColour);
        doLog("Colour 2 = " + secondColour);
        doLog("Colour 3 = " + thirdColour);
    }

    void CalculateDigitOrder()
    {
        List<string> productDigits = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            if (int.Parse(allTheDigits[i]) == 0)
            {
                productDigits.Add((1).ToString());
            }
            else
            {
                productDigits.Add(allTheDigits[i]);
            }
        }
        int productOfDigits = (int.Parse(productDigits[0]) * int.Parse(productDigits[1]) * int.Parse(productDigits[2]) * int.Parse(productDigits[3]));
        int timeNum = DateTime.Now.Hour * 100 + DateTime.Now.Minute;
        bool con1 = BombInfo.GetBatteryCount() > DateTime.Now.Month;
        bool con2 = productOfDigits > (BombInfo.GetModuleNames().Count() % 10);
        bool con3 = getTotalModuleCountByName("Colour Code") == 1;
        bool con4 = timeNum >= 300 && timeNum < 1600;
        bool con5 = BombInfo.GetModuleNames().Count() == 101 || BombInfo.GetModuleNames().Count() == 81;
        bool con6 = getTotalModuleCountByName("Colour Code") > Math.Sqrt(BombInfo.GetModuleNames().Count() / 2);
        bool con7 = BombInfo.GetSerialNumberLetters().Count() == 3;

        answerOrder.Clear();
        if (con1) answerOrder.Add("digit");
        if (con2) answerOrder.Add("colour");
        if (con3) answerOrder.Add("digit");
        if (con4) answerOrder.Add("colour");
        if (con5) answerOrder.Add("digit");
        if (con6) answerOrder.Add("colour");
        if (con7) answerOrder.Add("digit");

        if (!con7) answerOrder.Add("digit");
        if (!con6) answerOrder.Add("colour");
        if (!con5) answerOrder.Add("digit");
        if (!con4) answerOrder.Add("colour");
        if (!con3) answerOrder.Add("digit");
        if (!con2) answerOrder.Add("colour");
        if (!con1) answerOrder.Add("digit");

        doLog("Order of digits conditions = " + con1.ToString() + " " + con2.ToString() + " " + con3.ToString() + " " + con4.ToString() + " " + con5.ToString() + " " + con6.ToString() + " " + con7.ToString());
    }

    void PrepareCorrectAnswer()
    {
        finalText.Clear();

        int _dp = 1;
        int _cp = 1;

        string _logText = "Order of digits = ";

        for (int i = 0; i < answerOrder.Count; i++)
        {
            if (answerOrder[i] == "digit")
            {
                finalText.Add(_dp == 1 ? allTheDigits[0] : _dp == 2 ? allTheDigits[1] : _dp == 3 ? allTheDigits[2] : allTheDigits[3]);
                if (i != answerOrder.Count()) _logText += "d" + _dp.ToString() + ", ";
                _dp++;
            }
            else
            {
                finalText.Add(_cp == 1 ? allTheDigits[4] : _cp == 2 ? allTheDigits[5] : allTheDigits[6]);
                if (i != answerOrder.Count()) _logText += "c" + _cp.ToString() + ", ";
                _cp++;
            }
        }

        doLog(_logText);

        answerText = "";
        for (int i = 0; i < finalText.Count; i++)
        {
            string c = finalText[i];
            answerText += c == "orange" ? "o" : c == "blue" ? "b" : c == "red" ? "r" : c == "purple" ? "p" : c == "yellow" ? "y" : c == "green" ? "g" : c;
        }

        doLog("Correct answer is: " + answerText);
    }

    void PressNumberedButton(int buttonId)
    {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch();

        if (moduleSolved)
        {
            return;
        }

        string buttonText = NumberedButtons[buttonId].gameObject.name.Replace("Button", "");

        string myTextSpliting = "";
        for (int i = 0; i < myText.Length; i++)
        {
            myTextSpliting += myText[i] + ".";
        }
        string[] myTextSplit = myTextSpliting.Split('.');

        if (ArrayCountAnArray(myTextSplit, "0.1.2.3.4.5.6.7.8.9".Split('.')) == 2)
        {
            int TimerAsInteger = int.Parse(BombInfo.GetTime().ToString().Split('.')[0]);
            if (((TimerAsInteger % 60) % 10).ToString() == buttonText)
            {
                myText += buttonText;
            }
            else
            {
                doLog("Press this digit when the last digit of the seconds is equal to it");
                BombModule.HandleStrike();
            }
        }
        else
        {
            myText += buttonText;
        }

        if (myText.Length > 7)
        {
            myText = myText.Remove(myText.Length - 1, 1);
        }

        PrepareRenderReadyText();
        RenderScreen();
    }

    void PressColouredButton(int buttonId)
    {
        BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        ModuleSelect.AddInteractionPunch();

        if (moduleSolved)
        {
            return;
        }

        myText += ColouredButtons[buttonId].gameObject.name.Replace("Button", "");

        if (myText.Length > 7)
        {
            myText = myText.Remove(myText.Length - 1, 1);
        }

        PrepareRenderReadyText();
        RenderScreen();
    }

    void PressSubmitButton()
    {
        doLog("You entered: |" + myText + "|");
        doLog("Correct answer: |" + answerText + "|");
        if (string.Equals(myText, answerText))
        {
            string myTextSpliting = "";
            for (int i = 0; i < myText.Length; i++)
            {
                myTextSpliting += myText[i] + ".";
            }
            string[] myTextSplit = myTextSpliting.Split('.');
            if (ArrayCount(myTextSplit, "0") == 1 && ArrayCount(myTextSplit, "p") == 1)
            {
                doLog("1 purple and 1 zero so submit can only be pressed on 40 or 04");
                int seconds = int.Parse((BombInfo.GetTime() % 60).ToString().Split('.')[0]);
                doLog("Current seconds figures are: " + seconds);
                if (seconds == 40 || seconds == 4)
                {
                    doLog("This is correct");
                    moduleSolved = true;
                    BombModule.HandlePass();
                    RenderScreen();
                }
                else
                {
                    doLog("Invalid it must say 40 or 04");
                    BombModule.HandleStrike();
                }
            }
            else
            {
                moduleSolved = true;
                BombModule.HandlePass();
                RenderScreen();
            }
        }
        else
        {
            BombModule.HandleStrike();
        }
    }

    void PressDeleteButton()
    {
        if (myText.Length > 0)
        {
            myText = myText.Remove(myText.Length - 1, 1);
            PrepareRenderReadyText();
            RenderScreen();
        }
    }



    int ArrayCount(string[] a, string b)
    {
        int o = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (string.Equals(a[i], b))
            {
                o++;
            }
        }
        return o;
    }

    int ArrayCountAnArray(string[] a, string[] b)
    {
        int o = 0;
        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < b.Length; j++)
            {
                if (string.Equals(a[i], b[j]))
                {
                    o++;
                    break;
                }
            }
        }
        return o;
    }

    void PrepareRenderReadyText()
    {
        myText = myText.ToLower();
        string myTextSpliting = "";
        for (int i = 0; i < myText.Length; i++)
        {
            myTextSpliting += myText[i] + ".";
        }
        string[] myTextSplit = myTextSpliting.Split('.');
        screenText.Clear();
        for (int i = 0; i < myTextSplit.Length; i++)
        {
            string c = myTextSplit[i];
            switch (c)
            {
                case "r":
                    screenText.Add(materialsLight[0]);
                    break;
                case "o":
                    screenText.Add(materialsLight[1]);
                    break;
                case "y":
                    screenText.Add(materialsLight[2]);
                    break;
                case "g":
                    screenText.Add(materialsLight[3]);
                    break;
                case "b":
                    screenText.Add(materialsLight[4]);
                    break;
                case "p":
                    screenText.Add(materialsLight[5]);
                    break;
                default:
                    screenText.Add(c);
                    break;
            }
        }
    }

    void RenderScreen()
    {
        for (int i = 0; i < screenPieces.Length; i++)
        {
            RenderBlock(i, " ");
        }
        nothingText.SetActive(false);
        if (moduleSolved)
        {
            screenText.Clear();
            screenText.Add("S");
            screenText.Add("o");
            screenText.Add("l");
            screenText.Add("v");
            screenText.Add("e");
            screenText.Add("d");
        }
        else if (myText.Length == 0)
        {
            screenText.Clear();
            nothingText.SetActive(true);
        }
        for (int i = 0; i < screenText.Count; i++)
        {
            if (screenText[i].GetType() == typeof(Material))
            {
                RenderBlock(i, (Material)screenText[i]);
            }
            else if (screenText[i].GetType() == typeof(string))
            {
                RenderBlock(i, (string)screenText[i]);
            }
            else
            {
                RenderBlock(i, " ");
            }
        }
    }

    void RenderBlock(int i, string m)
    {
        if (i >= 7) return;
        Transform pos = screenPieces[i].transform;
        pos.GetChild(0).gameObject.SetActive(true);
        pos.GetChild(1).gameObject.SetActive(false);
        pos.GetChild(0).gameObject.GetComponent<TextMesh>().text = m;
    }

    void RenderBlock(int i, Material m)
    {
        if (i >= 7) return;
        Transform pos = screenPieces[i].transform;
        pos.GetChild(0).gameObject.SetActive(false);
        pos.GetChild(1).gameObject.SetActive(true);
        pos.GetChild(1).gameObject.GetComponent<Renderer>().material = m;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Submit your answer with “!{0} press 1|R|Y|9|P|0s0|3 (add sX [where 'X' is a digit] to press the button when the last seconds digit of the bomb is 'X')”. Delete screen with “!{0} delete 5 (number of times to press the button)”. Submit answer with “!{0} go 40 (submit when the seconds of the bomb is the number)”. Cancel submitting the answer with “!{0} go cancel/c/stop (stop submitting the answer)”.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();

        yield return null;

        if (Regex.IsMatch(command, @"^press +[0-9roygbps|]+$"))
        {
            command = command.Substring(6).Trim();
            var presses = command.Split('|');

            for (var i = 0; i < presses.Length; i++)
            {
                KMSelectable pressButton;

                if (Regex.IsMatch(presses[i], @"^[0-9]$") || Regex.IsMatch(presses[i], @"^[0-9]s[0-9]$"))
                {
                    pressButton = NumberedButtons[int.Parse(presses[i].First().ToString())];

                    if (Regex.IsMatch(presses[i], @"^[0-9]s[0-9]$"))
                    {
                        string formattedTime;

                        do
                        {
                            formattedTime = BombInfo.GetFormattedTime();

                            if (BombInfo.GetTime() < 60f)
                                formattedTime = BombInfo.GetFormattedTime().Substring(0, 2);

                            yield return new WaitForSeconds(0.1f);
                        } while (int.Parse(formattedTime.Last().ToString()) != int.Parse(presses[i].Last().ToString()));
                    }
                }
                else if (Regex.IsMatch(presses[i], @"^[r|o|y|g|b|p]$"))
                {
                    var colorLetters = new[] { "r", "o", "y", "g", "b", "p" };
                    pressButton = ColouredButtons[Array.IndexOf(colorLetters, presses[i])];
                }
                else
                {
                    continue;
                }

                yield return pressButton;
                yield return new WaitForSeconds(0.1f);
                yield return pressButton;
            }
        }

        if (Regex.IsMatch(command, @"^delete [1-7]$"))
        {
            yield return null;
            for (var i = 0; i < int.Parse(command.Substring(7).Trim()); i++)
            {
                yield return deleteButton;
                yield return new WaitForSeconds(0.1f);
                yield return deleteButton;
            }
        }

        if (Regex.IsMatch(command, @"^go \d\d$"))
        {
            command = command.Substring(3);

            if (int.Parse(command) < 60)
            {
                string formattedTime;

                do
                {
                    formattedTime = BombInfo.GetFormattedTime();

                    if (BombInfo.GetTime() < 60f)
                    {
                        formattedTime = BombInfo.GetFormattedTime().Substring(0, 2);
                    }
                    else
                    {
                        formattedTime = formattedTime.Substring(formattedTime.Length - 2, 2);
                    }

                    yield return "trywaitcancel 0.5 Stopped the go command";
                } while (int.Parse(formattedTime) != int.Parse(command.ToString()) && !cancelGoCommand);

                if (cancelGoCommand)
                {
                    doLog("Caught stop request");
                    cancelGoCommand = false;
                    yield return null;
                }
                else
                {
                    yield return submitButton;
                    yield return new WaitForSeconds(0.1f);
                    yield return submitButton;
                }
            }
        }

        yield break;
    }
}
