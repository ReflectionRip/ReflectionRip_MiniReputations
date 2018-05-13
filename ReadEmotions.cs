// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.Kindle
// Assembly: Assembly-CSharp, Version=2.0.6699.23498, Culture=neutral, PublicKeyToken=null
// MVID: F9D60392-97A0-46F2-94D9-101A168D4283
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll

using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.UI;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_ReadEmotions : BaseMutation
    {
        public Guid ReadEmotionsActivatedAbilityID = Guid.Empty;
        public ActivatedAbilityEntry ReadEmotionsActivatedAbility;
        public int cooldown = 0;
        public bool showFriendy = false;
        public bool showNumbers = false;

        public rr_ReadEmotions()
        {
            Name = nameof(rr_ReadEmotions);
            DisplayName = "Read Emotions";
            Type = "Mental";
        }

        public override bool CanLevel()
        {
            return false;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "CommandReadEmotions");
        }

        public override string GetDescription()
        {
            return "You read the targets emotional response to others.";
        }

        public override string GetLevelText(int Level)
        {
            return "Cooldown: " + cooldown + "\n" + "Range: 12";
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "CommandReadEmotions")
            {
                Cell C = PickDestinationCell(12, AllowVis.Any, false);
                if (C == null) return false;
                if (ParentObject.pPhysics.CurrentCell.DistanceTo(C) > 12)
                {
                    if (ParentObject.IsPlayer()) Popup.Show("That it out of range (maximum 12)", true);
                    return false;
                }
                ReadEmotionsActivatedAbility.Cooldown = 0;
                this.UseEnergy(1000);

                string output = string.Empty;
                foreach (GameObject gameObject in C.GetObjectsWithPart("Brain"))
                {
                    output = "Reading the brain of " + gameObject.DisplayName;
                    XRLCore.Core.Game.Player.Messages.Add(output);

                    GameObjectBlueprint myGOB = gameObject.GetBlueprint();
                    output = gameObject.DisplayName + " is a " + myGOB.Inherits;
                    XRLCore.Core.Game.Player.Messages.Add(output);

                    string targetsFaction = gameObject.pBrain.GetPrimaryFaction();

                    output = gameObject.DisplayName + " primary faction is " + Faction.getFormattedName(targetsFaction);
                    XRLCore.Core.Game.Player.Messages.Add(output);

                    foreach (KeyValuePair<string, Faction> item in Factions.FactionList)
                    {
                        bool modified = false;
                        int factionAmount = Factions.GetFeelingFactionToFaction(targetsFaction, item.Value.Name);
                        if (gameObject.pBrain.FactionFeelings.ContainsKey(item.Value.Name))
                        {
                            factionAmount = gameObject.pBrain.FactionFeelings[item.Value.Name];
                            modified = true;
                        }

                        if (factionAmount == 0) continue;
                        if (factionAmount > 0 && showFriendy == false) continue;

                        output = Faction.getFormattedName(item.Value.Name);
                        if (factionAmount < 0) output = "&rattack " + output + "&y";
                        else                   output = "&gignore " + output + "&y";

                        output = gameObject.DisplayName + " will " + output + " faction members";

                        if (showNumbers)
                        {
                            if (factionAmount < 0)
                            {
                                if (modified) output += ": [&r" + factionAmount + "&y]";
                                else output += ": &r" + factionAmount + "&y";
                            }
                            else
                            {
                                if (modified) output += ": [&g" + factionAmount + "&y]";
                                else output += ": &g" + factionAmount + "&y";
                            }
                        }
                        output += ".";

                        XRLCore.Core.Game.Player.Messages.Add(output);
                    }
                }
            }
            return true;
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);
            ActivatedAbilities part = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
            ReadEmotionsActivatedAbilityID = part.AddAbility(DisplayName, "CommandReadEmotions", "Mental Mutation", "#");
            ReadEmotionsActivatedAbility = part.AbilityByGuid[ReadEmotionsActivatedAbilityID];
            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            if (ReadEmotionsActivatedAbilityID != Guid.Empty)
            {
                ActivatedAbilities part = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
                part.RemoveAbility(ReadEmotionsActivatedAbilityID);
                ReadEmotionsActivatedAbilityID = Guid.Empty;
            }
            return true;
        }
    }
}
