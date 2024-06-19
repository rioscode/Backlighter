using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Backlighter.Properties;
using Device.Net;
using Hid.Net.Windows;

namespace Backlighter;

public class AppContext : ApplicationContext
{
    // Signal data to turn on or off backlighting on MX Keys
    private readonly byte[] _offSignal = [0x10, 0x01, 0x0b, 0x1f, 0x00, 0x00, 0xff];
    private readonly byte[] _onSignal = [0x10, 0x01, 0x0b, 0x1f, 0x01, 0x00, 0xff];

    private readonly NotifyIcon _trayIcon;
    private IDevice _unifyingReceiver;

    public AppContext()
    {
        var exitMenu = new ToolStripMenuItem("Exit");
        exitMenu.Click += CloseApplication;

        var mainMenu = new ContextMenuStrip();
        mainMenu.Items.AddRange(new ToolStripItem[]
        {
            exitMenu
        });

        _trayIcon = new NotifyIcon
        {
            Icon = Resources.keyboard,
            ContextMenuStrip = mainMenu,
            Visible = true
        };

        _ = Start();
    }

    private async Task Start()
    {
        // Register the factory for creating Hid devices. 
        var hidFactory = new FilterDeviceDefinition(
                vendorId: 0x046d,
                productId: 0xc52b,
                label: "Logitech Unifying Receiver",
                usagePage: 65280)
            .CreateWindowsHidDeviceFactory();

        // Get connected device definitions
        var deviceDefinitions = await hidFactory.GetConnectedDeviceDefinitionsAsync();
        var deviceDefinition = deviceDefinitions.FirstOrDefault();
        if (deviceDefinition == null)
            return;

        // Get the device from its definition
        _unifyingReceiver = await hidFactory.GetDeviceAsync(deviceDefinition);

        // Initialize the device
        await _unifyingReceiver.InitializeAsync();

        // Send the signals to turn off and on the backlighting
        while (true)
        {
            await _unifyingReceiver.WriteAsync(_offSignal).ConfigureAwait(false);
            Thread.Sleep(50);
            await _unifyingReceiver.WriteAsync(_onSignal).ConfigureAwait(false);
            Thread.Sleep(180000);
        }
    }

    private void CloseApplication(object sender, EventArgs e)
    {
        if (_unifyingReceiver != null)
            _unifyingReceiver.Dispose();

        if (_trayIcon != null)
            _trayIcon.Visible = false;

        Application.Exit();
    }
}