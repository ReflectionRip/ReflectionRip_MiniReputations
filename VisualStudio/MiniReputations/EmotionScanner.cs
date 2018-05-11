// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.DecoyHologramEmitter
// Assembly: Assembly-CSharp, Version=2.0.6699.298, Culture=neutral, PublicKeyToken=null
// MVID: 2D8F790D-8F35-4FD8-8303-49EC9406D41A
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll

using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.UI;

namespace XRL.World.Parts
{
    [Serializable]
    public class rr_EmotionScanner : IPart
    {
        public int ChargeUse = 1;

        public rr_EmotionScanner()
        {
            Name = nameof (rr_EmotionScanner);
        }

        public void ScanTarget()
        {
            string description = string.Empty;
            Physics tempPhysics = ParentObject.pPhysics;
            if (tempPhysics.Equipped == null) return;
            if (tempPhysics.Equipped.pPhysics.CurrentCell.ParentZone.IsWorldMap()) return;
            if (!tempPhysics.Equipped.IsPlayer()) return;

            Cell targetCell = tempPhysics.Equipped.pPhysics.PickDestinationCell(10, AllowVis.OnlyVisible, false);
            if (targetCell == null) return;

            string output = string.Empty;
            foreach (GameObject gameObject in targetCell.GetObjectsWithPart("Brain"))
            {
                output = "Reading the brain of " + gameObject.DisplayName;
                XRLCore.Core.Game.Player.Messages.Add(output);

                GameObjectBlueprint myGOB = gameObject.GetBlueprint();
                output = "The target is a " + myGOB.Inherits;
                XRLCore.Core.Game.Player.Messages.Add(output);

                string targetsFaction = gameObject.pBrain.GetPrimaryFaction();

                output = "The targets primary faction is " + Faction.getFormattedName(targetsFaction);
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

                    output = Faction.getFormattedName(item.Value.Name) + ": ";
                    if (factionAmount < 0)
                    {
                        if (modified) output += "[&r" + factionAmount + "&y]";
                        else          output += "&r" + factionAmount + "&y";
                    }
                    else
                    {
                        if (modified) output += "[&g" + factionAmount + "&y]";
                        else          output += "&g" + factionAmount + "&y";
                    }

                    XRLCore.Core.Game.Player.Messages.Add(output);
                }
            }
        }

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "GetInventoryActions");
            Object.RegisterPartEvent(this, "InvCommandActivate");
            Object.RegisterPartEvent(this, "BootSequenceInitialized");
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "GetInventoryActions")
            {
                EventParameterGetInventoryActions IA = E.GetParameter("Actions") as EventParameterGetInventoryActions;
                if (ParentObject.pPhysics.Equipped != null) IA.AddAction("Activate", 'a', false, "&Wa&yctivate", "InvCommandActivate");
                return true;
            }
            if (E.ID == "InvCommandActivate")
            {
                if (ParentObject.pPhysics.Equipped == null) return false;

                BootSequence part = ParentObject.GetPart<BootSequence>();
                if (part != null && part.BootTimeLeft > 0)
                {
                    if (ParentObject.pPhysics.Equipped.IsPlayer())
                    Popup.Show(ParentObject.The + ParentObject.DisplayNameOnly + " " + ParentObject.Is + " unresponsive.", true);
                }
                else if (ParentObject.UseCharge(ChargeUse))
                {
                    ScanTarget();
                }
                else
                {
                    if (ParentObject.pPhysics.Equipped.IsPlayer()) Popup.Show("This cell does not contain enough charge to execute the scan.", true);
                }

                return true;
            }

            return base.FireEvent(E);
        }
    }
}
