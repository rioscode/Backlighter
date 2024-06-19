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
    private NotifyIcon _trayIcon;
    private IDevice _unifyingReceiver;

    public AppContext()
    {
        var exitMenu = new ToolStripMenuItem("Exit");
        exitMenu.Click += ClickExit;

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
        var deviceDefinitions = (await hidFactory.GetConnectedDeviceDefinitionsAsync())
            .ToList();
        if (deviceDefinitions.Count == 0)
            return;

        // Get the device from its definition
        _unifyingReceiver = await hidFactory
            .GetDeviceAsync(deviceDefinitions.First());

        // Initialize the device
        await _unifyingReceiver.InitializeAsync();

        // Signal data to turn on or off backlighting on MX Keys
        var off_signal = new byte[]
        {
            0x10, 0x01, 0x0b, 0x1f, 0x00, 0x00, 0xff
        };

        var on_signal = new byte[]
        {
            0x10, 0x01, 0x0b, 0x1f, 0x01, 0x00, 0xff
        };

        // Send the signals to turn off and on the backlighting
        while (true)
        {
            await _unifyingReceiver.WriteAsync(off_signal).ConfigureAwait(false);
            Thread.Sleep(50);
            await _unifyingReceiver.WriteAsync(on_signal).ConfigureAwait(false);
            Thread.Sleep(180000);
        }
    }

    private void ClickExit(object sender, EventArgs e)
    {
        if (_unifyingReceiver != null)
            _unifyingReceiver.Dispose();

        if (_trayIcon != null)
            _trayIcon.Visible = false;

        Application.Exit();
    }
}