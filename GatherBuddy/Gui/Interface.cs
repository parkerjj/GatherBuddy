﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using GatherBuddy.Plugin;
using GatherBuddy.Time;
using ImGuiNET;
using ImGuiOtter;

namespace GatherBuddy.Gui;

public partial class Interface : Window, IDisposable
{
    private const string PluginName = "GatherBuddy";
    private const float  MinSize    = 700;

    private static GatherBuddy _plugin                 = null!;
    private        TimeStamp   _earliestKeyboardToggle = TimeStamp.Epoch;

    
    private static List<ExtendedFish>? _extendedFishList;


    public static IReadOnlyList<ExtendedFish> ExtendedFishList
        => _extendedFishList ??= GatherBuddy.GameData.Fishes.Values
            .Where(f => f.FishingSpots.Count > 0 && f.InLog)
            .Select(f => new ExtendedFish(f)).ToList();


    public Interface(GatherBuddy plugin)
        : base("GatherBuddy_CN")
    {
        _plugin            = plugin;
        _gatherGroupCache  = new GatherGroupCache(_plugin.GatherGroupManager);
        _gatherWindowCache = new GatherWindowCache();
        _locationTable     = new LocationTable();
        _alarmCache        = new AlarmCache(_plugin.AlarmManager);
        _recordTable       = new RecordTable();
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(MinSize,     17 * ImGui.GetTextLineHeightWithSpacing() / ImGuiHelpers.GlobalScale),
            MaximumSize = new Vector2(MinSize * 4, ImGui.GetIO().DisplaySize.Y * 15 / 16 / ImGuiHelpers.GlobalScale),
        };
        IsOpen = GatherBuddy.Config.OpenOnStart;
    }

    public override void PreDraw()
    {
        SetFlags();
    }

    public override void Draw()
    {
        SetupValues();
        DrawHeader();
        if (!ImGui.BeginTabBar("ConfigTabs###GatherBuddyConfigTabs", ImGuiTabBarFlags.Reorderable))
            return;

        using var end = ImGuiRaii.DeferredEnd(ImGui.EndTabBar);
        DrawItemTab();
        DrawFishTab();
        DrawWeatherTab();
        DrawAlarmTab();
        DrawGatherGroupTab();
        DrawGatherWindowTab();
        DrawConfigTab();
        DrawLocationsTab();
        DrawRecordTab();
        DrawDebugTab();
    }

    private void SetFlags()
    {
        if (GatherBuddy.Config.MainWindowLockPosition)
            Flags |= ImGuiWindowFlags.NoMove;
        else
            Flags &= ~ImGuiWindowFlags.NoMove;

        if (GatherBuddy.Config.MainWindowLockResize)
            Flags |= ImGuiWindowFlags.NoResize;
        else
            Flags &= ~ImGuiWindowFlags.NoResize;
    }

    public override void PreOpenCheck()
    {
        if (_earliestKeyboardToggle > GatherBuddy.Time.ServerTime || !Functions.CheckKeyState(GatherBuddy.Config.MainInterfaceHotkey, false))
            return;

        _earliestKeyboardToggle = GatherBuddy.Time.ServerTime.AddMilliseconds(500);
        Toggle();
    }

    public void Dispose()
    {
        _headerCache.Dispose();
        _weatherTable.Dispose();
        _itemTable.Dispose();
    }
}
