﻿using Newtonsoft.Json;
using PoryMoves.entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static moveParser.data.Move;
using hap = HtmlAgilityPack;

namespace moveParser.data
{
    public class MonData
    {
        public List<LevelUpMove> LevelMoves = new List<LevelUpMove>();
        public List<string> TMMoves = new List<string>();
        public List<string> EggMoves = new List<string>();
        public List<string> TutorMoves = new List<string>();
    }
    public class MonName
    {
        public int NatDexNum;
        public string SpeciesName;
        public bool IsBaseForm;
        public bool CanHatchFromEgg;
        public string FormName;
        public string VarName;
        public string DefName;
        public MonName(int nat, string og, bool isfrm, string formtm, string var, string def)
        {
            NatDexNum = nat;
            SpeciesName = og;
            IsBaseForm = isfrm;
            FormName = formtm;
            VarName = var;
            DefName = def;
        }
    }

    public class PokemonData
    {
        public static Dictionary<string, MonData> GetMonDataFromFile(string filedir)
        {
            Dictionary<string, MonData> dict;
            if (!File.Exists(filedir))
                return null;
            string text = File.ReadAllText(filedir);

            dict = JsonConvert.DeserializeObject<Dictionary<string, MonData>>(text);


            return dict;
        }

        public static List<MonName> GetMonNamesFromFile(string filedir)
        {
            List<MonName> list;
            string text = File.ReadAllText(filedir);

            list = JsonConvert.DeserializeObject<List<MonName>>(text);


            return list;
        }

        public static MonData LoadMonData(MonName name, GenerationData gen, Dictionary<string, Move> MoveData)
        {
            if (gen.dbFilename.Equals("lgpe") && (name.NatDexNum > 151 && (name.NatDexNum != 808 && name.NatDexNum != 809)))
                return null;

            MonData mon = new MonData();

            List<LevelUpMove> lvlMoves = new List<LevelUpMove>();
            //List<LevelUpMoveId> lvlMovesId = new List<LevelUpMoveId>();


            List<Move> TMMovesNew = new List<Move>();
            List<Move> EggMovesNew = new List<Move>();
            List<Move> TutorMovesNew = new List<Move>();

            if (gen.genNumber < 7 && name.FormName.Contains("Alola"))
                return null;

            if (gen.maxDexNum < name.NatDexNum)
                return null;

            string html = "https://pokemondb.net/pokedex/" + name.SpeciesName + "/moves/" + gen.genNumber;
            //string html = "https://pokemondb.net/pokedex/sneasel/moves/8";

            hap.HtmlWeb web = new hap.HtmlWeb();
            hap.HtmlDocument htmlDoc;
            try
            {
                htmlDoc = web.Load(html);
                htmlDoc.DocumentNode.InnerHtml = htmlDoc.DocumentNode.InnerHtml.Replace("\n", "").Replace("> <", "><");
            }
            catch (System.Net.WebException)
            {
                return null;
            }
            
            hap.HtmlNodeCollection columns;

            columns = htmlDoc.DocumentNode.SelectNodes("//div[@class='tabset-moves-game sv-tabs-wrapper']");

            int column = 0;
            int gamecolumnamount = 1;
            int movetutorcolumn = gen.moveTutorColumn;
            string gameAbv = gen.lvlUpColumn;
            string gametosearch = gen.gameFullName;

            if (columns != null)
            {
                bool inList = false;
                bool readingLearnsets = !gen.isLatestGen;
                bool readingLevelUp = false;
                bool LevelUpListRead = false;
                bool TMListRead = false;
                bool EggListRead = false;
                bool TutorListRead = false;
                string pagetext = columns[0].InnerHtml.Replace("&lt;br>\n", "&lt;br>");
                string gameText = null, modeText = null, formText = null;

                int tabNum = 1;
                foreach (hap.HtmlNode nodo1 in columns[0].ChildNodes)
                {
                    if (nodo1.Attributes["class"].Value.Equals("sv-tabs-tab-list"))
                    {
                        foreach (hap.HtmlNode nodo2 in nodo1.ChildNodes)
                        {
                            if (nodo2.InnerText.Equals(gen.gameAvailableName))
                                break;
                            tabNum++;
                        }
                    }
                    else if (nodo1.Attributes["class"].Value.Equals("sv-tabs-panel-list"))
                    {
                        hap.HtmlNode nodo2 = nodo1.ChildNodes[tabNum - 1];
                        foreach(hap.HtmlNode nodo3 in nodo2.ChildNodes)
                        {
                            if (nodo3.Attributes["class"].Value.Equals("grid-row"))
                            {
                                foreach(hap.HtmlNode nodo4 in nodo3.ChildNodes)
                                {
                                    for(int i = 0; i < nodo4.ChildNodes.Count / 3; i++)
                                    {
                                        if (nodo4.ChildNodes[i * 3].InnerText.Equals("Moves learnt by level up"))
                                        {
                                            foreach(hap.HtmlNode levelRow in nodo4.ChildNodes[i * 3 + 2].ChildNodes[0].ChildNodes[1].ChildNodes)
                                            {
                                                int lvl = int.Parse(levelRow.ChildNodes[0].InnerText);
                                                string movename = levelRow.ChildNodes[1].InnerText;
                                                Move mo = MoveData[movename];
                                                lvlMoves.Add(new LevelUpMove(lvl, "MOVE_" + mo.defineName));
                                            }
                                        }
                                        else if (nodo4.ChildNodes[i * 3].InnerText.Equals("Moves learnt by level up"))
                                        {
                                            foreach (hap.HtmlNode levelRow in nodo4.ChildNodes[i * 3 + 2].ChildNodes[0].ChildNodes[1].ChildNodes)
                                            {
                                                int lvl = int.Parse(levelRow.ChildNodes[0].InnerText);
                                                string movename = levelRow.ChildNodes[1].InnerText;
                                                Move mo = MoveData[movename];
                                                lvlMoves.Add(new LevelUpMove(lvl, "MOVE_" + mo.defineName));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                int rownum = 0;
                List<Move> evoMovesId = new List<Move>();
                /*

                foreach (string textRow in pagetext.Split('\n'))
                {
                    bool gameTextException = false;

                    switch (gen.genNumber)
                    {
                        case 1:
                            if (name.SpeciesName.Equals("Vaporeon"))
                                gameTextException = true;
                            break;
                        case 2:
                            if (name.SpeciesName.Equals("Muk"))
                                gameTextException = true;
                            break;
                        case 4:
                            if (textRow.Contains("{{tt|60|70 in Pokémon Diamond and Pearl and Pokémon Battle Revolution}}"))
                                gameTextException = true;
                            break;
                    }

                    if (name.SpeciesName.Equals("Bonsly"))
                        gameTextException = true;

                    if (readingLearnsets && textRow.Contains("Pokémon") && !gameTextException)
                        gameText = textRow;
                    else if (textRow.Contains("=Learnset="))
                        readingLearnsets = true;
                    else if (textRow.Contains("=Side game data="))
                        readingLearnsets = false;

                    if (readingLearnsets && !textRow.Trim().Equals(""))
                    {
                        rownum++;
                        if (textRow.ToLower().Contains("{{learnlist/movena|"))
                            return null;
                        else if (textRow.ToLower().Contains("by [[level|leveling"))
                            modeText = "Level";
                        else if (textRow.Contains("By [[TM]]"))
                            modeText = "TM";
                        else if (textRow.Contains("By {{pkmn|breeding}}"))
                            modeText = "EGG";
                        else if (textRow.Contains("By [[transfer]] from"))
                            modeText = "TRANSFER";
                        else if (gen.moveTutorColumn != 0 && textRow.ToLower().Contains("by [[move tutor|tutoring]]"))
                            modeText = "TUTOR";
                        else if (textRow.Contains("====") && !readingLevelUp && !textRow.Contains("Pokémon")
                            && !textRow.Contains("By a prior [[evolution]]") && !textRow.Contains("Special moves") && !textRow.Contains("By {{pkmn2|event}}s"))
                            formText = Regex.Replace(textRow.Replace("=", ""), "{{sup([^{]*)([A-Z][a-z]*)}}", "");

                        else if (textRow.ToLower().Contains("{{learnlist/levelh")
                            || textRow.ToLower().Contains("{{learnlist/tmh")
                            || textRow.ToLower().Contains("{{learnlist/breedh")
                            || textRow.ToLower().Contains("{{learnlist/tutorh"))
                        {
                            if (modeText == null)
                                continue;
                            if (matchForm(formText, name.FormName) && (gameText == null || gameText.Contains(gametosearch)))
                            {
                                if (modeText.Equals("Level") && !LevelUpListRead)
                                {
                                    inList = true;
                                    readingLevelUp = true;
                                    string[] rowdata = textRow.Split('|');

                                    int toMinus = 0;

                                    for (int i = 0; i < rowdata.Length; i++)
                                    {
                                        string header = rowdata[i].Replace("}", "");
                                        int a;

                                        if (header.Contains("xy=") || header.Equals("") || int.TryParse(header, out a))
                                            toMinus++;
                                        if (header.Equals("V"))
                                        {
                                            toMinus--;
                                            if (gen.lvlUpColumn.Equals("BW"))
                                                column = 1;
                                            else
                                                column = 2;
                                        }

                                        if (header.Equals(gameAbv))
                                        {
                                            column = i - 3 - toMinus;
                                        }
                                    }

                                    gamecolumnamount = rowdata.Length - 4 - toMinus;

                                    if (gamecolumnamount <= 0)
                                        gamecolumnamount = 1;

                                    if (column == 0)
                                        column = 1;
                                }
                                else if ((modeText.Equals("TM") && !TMListRead) || (modeText.Equals("EGG") && !EggListRead) || (modeText.Equals("TUTOR") && !TutorListRead))
                                {
                                    inList = true;
                                }
                            }

                        }
                        else if (textRow.ToLower().Contains("{{learnlist/levelf") && (gameText == null || gameText.Contains(gametosearch)))
                        {
                            inList = false;
                            if (formText == null || formText.Equals(name.FormName))
                                LevelUpListRead = true;
                            formText = null;
                            readingLevelUp = false;
                        }
                        else if (textRow.ToLower().Contains("{{learnlist/tmf") && (gameText == null || gameText.Contains(gametosearch)))
                        {
                            inList = false;
                            if (formText == null || formText.Equals(name.FormName))
                                TMListRead = true;
                            formText = null;
                        }
                        else if (textRow.ToLower().Contains("{{learnlist/breedf") && (gameText == null || gameText.Contains(gametosearch)))
                        {
                            inList = false;
                            if (formText == null || formText.Equals(name.FormName))
                                EggListRead = true;
                            formText = null;
                        }
                        else if (textRow.ToLower().Contains("{{learnlist/tutorf") && (gameText == null || gameText.Contains(gametosearch)))
                        {
                            inList = false;
                            if (formText == null || formText.Equals(name.FormName))
                                TutorListRead = true;
                            formText = null;
                        }
                        else if (inList && (gameText == null || gameText.Contains(gametosearch)))
                        {
                            if (modeText.Equals("Level") && !LevelUpListRead && (formText == null || formText.Equals(name.FormName)))
                            {
                                string lvltext = textRow.Replace("{{tt|Evo.|Learned upon evolving}}", "0");
                                lvltext = lvltext.Replace("{{tt|60|70 in Pokémon Diamond and Pearl and Pokémon Battle Revolution}}", "60");
                                string[] rowdata = System.Text.RegularExpressions.Regex.Replace(lvltext, "{{tt([^}]+)}}", "").Split('|');
                                string lvl = rowdata[column].Replace("*", "");
                                string movename = rowdata[gamecolumnamount + 1];

                                if (!lvl.Equals("N/A"))
                                {
                                    Move mo = MoveData[movename];

                                    if (mo.moveId == 617)
                                    {
                                        if (name.SpeciesName.Equals("FLOETTE_ETERNAL_FLOWER"))
                                            lvlMoves.Add(new LevelUpMove(int.Parse(lvl), "MOVE_" + mo.defineName));
                                    }
                                    else
                                    {
                                        if (lvl.Equals("0"))
                                            evoMovesId.Add(mo);
                                        else
                                            lvlMoves.Add(new LevelUpMove(int.Parse(lvl), "MOVE_" + mo.defineName));
                                    }
                                }
                            }
                            else if (modeText.Equals("TM") && !TMListRead && (formText == null || formText.Equals(name.FormName)) && !Regex.IsMatch(textRow.ToLower(), "{{learnlist/t[mr].+null}}"))
                            {
                                string[] rowdata = textRow.Split('|');
                                string movename = rowdata[2];

                                //TMMovesIds.Add(SerebiiNameToID[movename]);
                                try
                                {
                                    TMMovesNew.Add(MoveData[movename]);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("There's no move data in db/moveNames.json for " + movename + ". Skipping move.", "Missing Data", MessageBoxButtons.OK);
                                }
                            }
                            else if (modeText.Equals("EGG") && !EggListRead && (formText == null || formText.Equals(name.FormName)) && !Regex.IsMatch(textRow.ToLower(), "{{learnlist/breed.+null"))
                            {
                                string breedtext = textRow.Replace("{{tt|*|No legitimate means to pass down move}}", "");
                                breedtext = breedtext.Replace("{{tt|*|Male-only, and none of the evolutions can learn this move legitimately}}", "");
                                breedtext = breedtext.Replace("{{tt|*|No legitimate father to pass down move}}", "");
                                breedtext = breedtext.Replace("{{tt|*|No legitimate means to pass down the move}}", "");
                                breedtext = breedtext.Replace("{{tt|*|Paras learns Sweet Scent as an Egg move in Gold and Silver; in Crystal, the only fathers that can be learn the move learn it via TM}}", "");
                                string[] rowdata = System.Text.RegularExpressions.Regex.Replace(breedtext, "{{sup(.*)\v([A-Z]*)}}|{{MS([^}]+)}}", "MON").Split('|');
                                string movename = rowdata[2];

                                if (gen.genNumber == 4 && rowdata.Length >= 13 && !rowdata[12].Trim().Equals("") && !breedtext.Contains(gen.lvlUpColumn))
                                    continue;
                                else if (Regex.IsMatch(breedtext, "{{sup(.*)\\\u007C([A-Z]*)}}") && !breedtext.Contains(gen.dbFilename.ToUpper()))
                                    continue;
                                else if (!movename.Equals("Light Ball}}{{tt") && !(textRow.Contains("†") && !isIncenseBaby(name.SpeciesName)))
                                    //EggMovesIds.Add(SerebiiNameToID[movename]);
                                    EggMovesNew.Add(MoveData[movename]);
                            }
                            else if (modeText.Equals("TUTOR") && !TutorListRead && !Regex.IsMatch(textRow.ToLower(), "{{learnlist/tutor.+null}}")
                                && matchForm(formText, name.FormName))
                            {
                                string tutortext = textRow.Replace("{{tt|*|", "");
                                string[] rowdata = System.Text.RegularExpressions.Regex.Replace(tutortext, "}}", "").Split('|');
                                //if 
                                string movename = rowdata[1];
                                try
                                {
                                    int tutorpad;
                                    if (gen.genNumber == 3 || gen.genNumber == 4)
                                        tutorpad = 10;
                                    else
                                        tutorpad = 8;

                                    if (gen.genNumber == 1 || gen.genNumber == 2)
                                    {
                                        //int modeid = SerebiiNameToID[movename];
                                        //TutorMovesIds.Add(modeid);
                                        TutorMovesNew.Add(MoveData[movename]);
                                    }
                                    else if (rowdata[tutorpad + movetutorcolumn].Equals("yes"))
                                    {
                                        Move mov = MoveData[movename];
                                        if (mov.moveId == 520 && name.SpeciesName.Equals("Silvally"))
                                        {
                                            //TutorMovesIds.Add(518);
                                            //TutorMovesIds.Add(519);
                                            TutorMovesNew.Add(MoveData["Water Pledge"]);
                                            TutorMovesNew.Add(MoveData["Fire Pledge"]);
                                        }
                                        TutorMovesNew.Add(mov);
                                    }
                                }
                                catch (IndexOutOfRangeException) { }
                            }

                            //for (int i = 0; )
                        }
                    }

                }
                */
                foreach (Move moe in evoMovesId)
                    lvlMoves.Insert(0,new LevelUpMove(0, "MOVE_" + moe.defineName));

                TMMovesNew = TMMovesNew.Distinct().ToList();
                EggMovesNew = EggMovesNew.Distinct().ToList();
                TutorMovesNew = TutorMovesNew.Distinct().ToList();
            }
            else
            {
                return null;
            }
            mon.LevelMoves = lvlMoves;
            foreach (Move m in TMMovesNew)
                mon.TMMoves.Add("MOVE_" + m.defineName);
            foreach (Move m in EggMovesNew)
                mon.EggMoves.Add("MOVE_" + m.defineName);
            foreach (Move m in TutorMovesNew)
                mon.TutorMoves.Add("MOVE_" + m.defineName);

            return mon;
        }
        public static bool isIncenseBaby(string name)
        {
            switch (name)
            {
                case "Munchlax":
                case "Budew":
                case "Bonsly":
                case "Happiny":
                case "Wynaut":
                case "Azurill":
                case "Mantyke":
                case "Chingling":
                case "Mime Jr.":
                    return true;
                default:
                    return false;
            }
        }

        public static bool matchForm(string currentForm, string formToCheck)
        {
            if (currentForm == null)
                return true;
            if (currentForm.Equals(formToCheck))
                return true;
            if (currentForm.Equals(formToCheck + " / Defense Forme"))
                return true;
            if (currentForm.Equals("Attack Forme / " + formToCheck))
                return true;
            return false;
        }
    }
}
