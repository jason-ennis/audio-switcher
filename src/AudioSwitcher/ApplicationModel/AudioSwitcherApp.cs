// -----------------------------------------------------------------------
// Copyright (c) David Kean.
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AudioSwitcher.Audio;
using AudioSwitcher.ComponentModel;

namespace AudioSwitcher.ApplicationModel
{
    // Represents the lifetime of the audio switcher application
    [Export(typeof(IApplication))]
    internal class AudioSwitcherApp : IApplication
    {
        private readonly Lazy<IStartupService, IPriorityMetadata>[] _startupServices;
        private readonly Queue<Action> _idleActions = new Queue<Action>();

        [ImportingConstructor]
        public AudioSwitcherApp([ImportMany]Lazy<IStartupService, IPriorityMetadata>[] startupServices)
        {
            _startupServices = startupServices.OrderBy(s => s.Metadata.Priority)
                                              .ToArray();
        }

        public string Title
        {
            get { return Resources.Title; }
        }

        public Icon NotificationAreaIcon
        {
            get { return Resources.NotificationArea; }
        }

        public string ExecutablePath
        {
            get { return Application.ExecutablePath; }
        }

        public IDictionary<string, string> Args { get; set; }

        public void Start()
        {
            //JRE just switch the device and end
            if (Args.Keys.Contains("switch"))
            {
                SwitchDevice();
                return;
            }

            // Some of the startup services expect, or require a SynchronizationContext, 
            // so we run them after the message loop has started.
            RunOnNextIdle(() => RunStartupServices());

            Application.Idle += OnApplicationIdle;
            Application.Run();
        }

        public void Shutdown()
        {
            Application.Exit();
            Application.Idle -= OnApplicationIdle;
        }

        public void RunOnNextIdle(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            _idleActions.Enqueue(action);
        }

        private void RunStartupServices()
        {
            foreach (var service in _startupServices)
            {
                if (!service.Value.Startup())
                {
                    Shutdown();
                    break;
                }
            }
        }

        private void OnApplicationIdle(object sender, EventArgs e)
        {
            if (_idleActions.Count > 0)
            {
                // Snapshot actions and then clear existing in
                // case one of actions queue additional work
                Action[] actions = _idleActions.ToArray();
                _idleActions.Clear();

                foreach (Action action in actions)
                {
                    action();
                }
            }
        }

        private void SwitchDevice()
        {
            var manager = new AudioDeviceManager();
            var devices = manager.GetAudioDevices(AudioDeviceKind.Playback, AudioDeviceState.Active);
            var current = manager.GetDefaultAudioDevice(AudioDeviceKind.Playback, AudioDeviceRole.Multimedia)
                .ToString()
                .ToUpper()
                .Replace(" ", "");

            var headphones = Args["headphones"]
                .ToUpper()
                .Replace(" ", "");

            var speakers = Args["speakers"]
                .ToUpper()
                .Replace(" ", "");

            AudioDevice device = null;
            if (current == headphones)
                device = devices.FirstOrDefault(d => d.ToString().ToUpper().Replace(" ", "") == speakers);
            else
                device = devices.FirstOrDefault(d => d.ToString().ToUpper().Replace(" ", "") == headphones);

            if (device != null)
                manager.SetDefaultAudioDevice(device, AudioDeviceRole.Multimedia);
        }
    }
}
