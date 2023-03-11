﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAPICodePack.Dialogs;
using sxlib;
using sxlib.Specialized;
using SynUI.Properties;

namespace SynUI.Services;

public interface ISynapseService
{
    SxLibBase.SynLoadEvents LoadState { get; }
    SxLibBase.SynAttachEvents AttachState { get; }

    RelayCommand<string> ExecuteCommand { get; }
    RelayCommand AttachCommand { get; }

    void Initialize();
}

public class SynapseService : ObservableObject, ISynapseService
{
    private SxLibWPF? _api;
    private SxLibBase.SynAttachEvents _attachState;
    private SxLibBase.SynLoadEvents _loadState;

    public SynapseService()
    {
        ExecuteCommand = new RelayCommand<string>(
            script => _api?.Execute(script),
            _ => true
                // AttachState is
                //     SxLibBase.SynAttachEvents.READY or
                //     SxLibBase.SynAttachEvents.ALREADY_INJECTED
                );

        AttachCommand = new RelayCommand(
            () => _api?.Attach(),
            () => LoadState == SxLibBase.SynLoadEvents.READY);
    }

    public RelayCommand<string> ExecuteCommand { get; }
    public RelayCommand AttachCommand { get; }

    public SxLibBase.SynLoadEvents LoadState
    {
        get => _loadState;
        private set
        {
            SetProperty(ref _loadState, value);
            ExecuteCommand.NotifyCanExecuteChanged();
            AttachCommand.NotifyCanExecuteChanged();
        }
    }

    public SxLibBase.SynAttachEvents AttachState
    {
        get => _attachState;
        private set
        {
            SetProperty(ref _attachState, value);
            ExecuteCommand.NotifyCanExecuteChanged();
            AttachCommand.NotifyCanExecuteChanged();
        }
    }

    public void Initialize()
    {
        _api = SxLib.InitializeWPF(Application.Current.MainWindow, Directory.GetCurrentDirectory());
        _api.LoadEvent += _sxlib_OnLoadEvent;
        _api.AttachEvent += _sxlib_OnAttachEvent;

        // Add autoexec script
        using var script = Assembly.GetExecutingAssembly().GetManifestResourceStream("SynUI.Resources.synui-auto-exec.lua");
        var autoexecPath = Path.Combine(Directory.GetCurrentDirectory(), "autoexec",
            "THIS FILE IS GENERATED BY SYNUI DO NOT REMOVE.lua");
        var file = File.Open(autoexecPath, FileMode.OpenOrCreate);
        script?.CopyTo(file);
        
        _api.Load();
    }

    private void _sxlib_OnAttachEvent(SxLibBase.SynAttachEvents @event, object param) =>
        Application.Current.Dispatcher.Invoke(() => AttachState = @event);

    private void _sxlib_OnLoadEvent(SxLibBase.SynLoadEvents @event, object param) =>
        Application.Current.Dispatcher.Invoke(() => LoadState = @event);
}