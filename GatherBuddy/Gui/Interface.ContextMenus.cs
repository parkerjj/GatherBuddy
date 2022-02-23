﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using GatherBuddy.Alarms;
using GatherBuddy.Classes;
using GatherBuddy.Config;
using GatherBuddy.GatherHelper;
using GatherBuddy.Interfaces;
using GatherBuddy.Plugin;
using GatherBuddy.Structs;
using ImGuiNET;
using ImGuiOtter;

namespace GatherBuddy.Gui;

public partial class Interface
{
    private const string AutomaticallyGenerated = "Automatically generated from context menu.";

    private void DrawAddAlarm(IGatherable item)
    {
        // Only timed items.
        if (item.InternalLocationId <= 0)
            return;

        var current = _alarmCache.Selector.EnsureCurrent();
        if (ImGui.Selectable("Add to Alarm Preset"))
        {
            if (current == null)
            {
                _plugin.AlarmManager.AddGroup(new AlarmGroup()
                {
                    Description = AutomaticallyGenerated,
                    Enabled     = true,
                    Alarms      = new List<Alarm> { new(item) { Enabled = true } },
                });
                current = _alarmCache.Selector.EnsureCurrent();
            }
            else
            {
                _plugin.AlarmManager.AddAlarm(current, new Alarm(item));
            }
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(
                $"Add {item.Name[GatherBuddy.Language]} to {(current == null ? "a new alarm preset." : CheckUnnamed(current.Name))}");
    }

    private void DrawAddToGatherGroup(IGatherable item)
    {
        var       current = _gatherGroupCache.Selector.EnsureCurrent();
        using var color   = ImGuiRaii.PushColor(ImGuiCol.Text, ColorId.DisabledText.Value(), current == null);
        if (ImGui.Selectable("Add to Gather Group") && current != null)
            if (_plugin.GatherGroupManager.ChangeGroupNode(current, current.Nodes.Count, item, null, null, null, false))
                _plugin.GatherGroupManager.Save();

        color.Pop();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(current == null
                ? "Requires a Gather Group to be setup and selected."
                : $"Add {item.Name[GatherBuddy.Language]} to {current.Name}");
    }

    private void DrawAddGatherWindow(IGatherable item)
    {
        var current = _gatherWindowCache.Selector.EnsureCurrent();

        if (ImGui.Selectable("Add to Gather Window Preset"))
        {
            if (current == null)
                _plugin.GatherWindowManager.AddPreset(new GatherWindowPreset
                {
                    Enabled     = true,
                    Items       = new List<IGatherable> { item },
                    Description = AutomaticallyGenerated,
                });
            else
                _plugin.GatherWindowManager.AddItem(current, item);
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(
                $"Add {item.Name[GatherBuddy.Language]} to {(current == null ? "a new gather window preset." : CheckUnnamed(current.Name))}");
    }

    private static string TeamCraftAddress(string type, uint id)
    {
        var lang = GatherBuddy.Language switch
        {
            ClientLanguage.English  => "en",
            ClientLanguage.German   => "de",
            ClientLanguage.French   => "fr",
            ClientLanguage.Japanese => "ja",
            _                       => "en",
        };

        return $"https://ffxivteamcraft.com/db/{lang}/{type}/{id}";
    }

    private static string TeamCraftAddress(FishingSpot s)
        => s.Spearfishing
            ? TeamCraftAddress("spearfishing-spot", s.SpearfishingSpotData!.GatheringPointBase.Row)
            : TeamCraftAddress("fishing-spot",      s.Id);

    private static string GarlandToolsItemAddress(uint itemId)
        => $"https://www.garlandtools.org/db/#item/{itemId}";

    private static void DrawOpenInGarlandTools(uint itemId)
    {
        if (itemId == 0)
            return;

        if (!ImGui.Selectable("Open in GarlandTools"))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(GarlandToolsItemAddress(itemId)) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            PluginLog.Error($"Could not open GarlandTools:\n{e.Message}");
        }
    }

    private static void DrawOpenInTeamCraft(uint itemId)
    {
        if (itemId == 0)
            return;
        if (!ImGui.Selectable("Open in TeamCraft"))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(TeamCraftAddress("item", itemId)) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            PluginLog.Error($"Could not open TeamCraft:\n{e.Message}");
        }
    }

    private static void DrawOpenInTeamCraft(FishingSpot fs)
    {
        if (fs.Id == 0)
            return;
        if (!ImGui.Selectable("Open in TeamCraft"))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(TeamCraftAddress(fs)) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            PluginLog.Error($"Could not open TeamCraft:\n{e.Message}");
        }
    }

    private void CreateContextMenu(IGatherable item)
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(item.Name[GatherBuddy.Language]);

        if (!ImGui.BeginPopup(item.Name[GatherBuddy.Language]))
            return;

        using var end = ImGuiRaii.DeferredEnd(ImGui.EndPopup);
        DrawAddAlarm(item);
        DrawAddToGatherGroup(item);
        DrawAddGatherWindow(item);
        if (ImGui.Selectable("Create Link"))
            Communicator.Print(SeString.CreateItemLink(item.ItemId));
        DrawOpenInGarlandTools(item.ItemId);
        DrawOpenInTeamCraft(item.ItemId);
    }

    private static void CreateContextMenu(Bait bait)
    {
        if (bait.Id == 0)
            return;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(bait.Name);

        if (!ImGui.BeginPopup(bait.Name))
            return;

        using var end = ImGuiRaii.DeferredEnd(ImGui.EndPopup);

        if (ImGui.Selectable("Create Link"))
            Communicator.Print(SeString.CreateItemLink(bait.Id));
        DrawOpenInGarlandTools(bait.Id);
        DrawOpenInTeamCraft(bait.Id);
    }

    private static void CreateContextMenu(FishingSpot? spot)
    {
        if (spot == null)
            return;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(spot.Name);

        if (!ImGui.BeginPopup(spot.Name))
            return;

        using var end = ImGuiRaii.DeferredEnd(ImGui.EndPopup);

        DrawOpenInTeamCraft(spot);
    }
}