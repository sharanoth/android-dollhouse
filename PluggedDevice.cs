using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace WpfApp1
{
    public class PluggedDevice
    {
        public string deviceModel = "No Device Detected.";
        public string deviceName ="";
        public string deviceStorage = "";
        public string deviceImei = "";
        public string deviceSerial = "";
        public string deviceCurrentMode = "Unplugged";
        public string deviceCurrentSlot = "";
        public bool deviceIsInBootloader = false;
        public string deviceCurrentVendor = "";
        public string deviceSELINUXStatus = "";
        public string deviceOSVersion = "";
        public bool deviceIsSamsung = false;
        public bool deviceIsHMOS = false;
        public string deviceKnoxBit = "";

        public PluggedDevice(Button b)
        {
            Process p = new Process();
            p.StartInfo.FileName = "./res/platform-tools/adb.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = "devices -l";
            b.Content = "Refreshing";
            //Get current mode
            p.Start();
            p.WaitForExitAsync();
            string output = p.StandardOutput.ReadToEnd().Remove(0,26).Trim();
            output = Regex.Replace(output, @"\s+", " ");
            string[] data = output.Split(" ").ToArray();
            try
            {
                deviceSerial = data[0];
                if (data[1] == "device")
                {
                    deviceCurrentMode = "System";
                }
                else if (data[1] == "recovery")
                {
                    deviceCurrentMode = "Recovery";
                }
            }
            catch (IndexOutOfRangeException)
            {
                p.WaitForExit();
                p.Kill();

                p.StartInfo.FileName = "./res/platform-tools/fastboot.exe";
                p.StartInfo.Arguments = "devices";
                p.Start();

                output = p.StandardOutput.ReadToEnd().Trim();
                output = Regex.Replace(output, @"\s+", " ");
                data = output.Split(" ").ToArray();
                try
                {
                    deviceSerial = data[0];
                    if (data[1] == "fastboot")
                    {
                        deviceCurrentMode = "Bootloader";
                    }

                }
                catch (IndexOutOfRangeException)
                {
                    deviceSerial = "";
                    deviceModel = "";
                    deviceName = "No Device Detected.";
                }

            }
            p.WaitForExitAsync();

            //get product.name(codename)

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "./res/platform-tools/adb.exe";
            p.StartInfo.Arguments = "shell getprop ro.product.name";
            p.Start();
            deviceName = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExitAsync();

            //get build.version

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "./res/platform-tools/adb.exe";
            p.StartInfo.Arguments = "shell getprop ro.build.version.release";
            p.Start();
            deviceOSVersion = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExitAsync();

            //get SDK version
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "./res/platform-tools/adb.exe";
            p.StartInfo.Arguments = "shell getprop ro.build.version.sdk";
            p.Start();
            var sdk = p.StandardOutput.ReadToEnd().Trim();
            deviceOSVersion += "(API " + sdk + ")";
            p.WaitForExitAsync();

            //get product.model

            p.StartInfo.Arguments = "shell getprop ro.product.model";
            p.Start();
            deviceModel = p.StandardOutput.ReadToEnd().Trim();
            if (deviceModel.StartsWith("SM-")) {
                deviceIsSamsung = true;

                // Detect warranty bit
                var bit = GetProp("ro.boot.warranty_bit");
                if (bit == "") {
                    bit = GetProp("ro.warranty_bit");
                }
                deviceKnoxBit = bit;
            }

            // TODO better HarmonyOS detection (I don't have a Huawei device)
            // This method assumes that every Huawei device running Android 10 is running HarmonyOS
            p.StartInfo.Arguments = "shell getprop ro.build.fingerprint";
            p.Start();
            var fp = p.StandardOutput.ReadToEnd().Trim();
            if (fp.StartsWith("HUAWEI") && sdk == "29") {
                deviceIsHMOS = true;
            }

            if (deviceCurrentMode != "Bootloader")
            {
                //get selinux 
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = "./res/platform-tools/adb.exe";
                p.StartInfo.Arguments = "shell";
                p.Start();
                p.StandardInput.WriteLine("getenforce");
                p.StandardInput.WriteLine("exit");
                output = p.StandardOutput.ReadToEnd().Trim();
                if (output == "")
                {
                    p.Start();
                    p.StandardInput.WriteLine("su");
                    p.StandardInput.WriteLine("getenforce");
                    p.StandardInput.WriteLine("exit");
                    output = p.StandardOutput.ReadToEnd().Trim();
                    if (output == "")
                    {
                        deviceSELINUXStatus = "Requires root";
                    }
                    else
                    {
                        deviceSELINUXStatus = output;
                    }
                    
                }
                else
                {
                    deviceSELINUXStatus = output;
                }
                p.WaitForExitAsync();

                //get imei
                if (deviceCurrentMode == "Recovery")
                {
                    deviceImei = "Inaccessible in TWRP";
                }
                else if (deviceCurrentMode == "System")
                {
                    p.Start();
                    p.StandardInput.WriteLine("service call iphonesubinfo 1 | cut -c 52-66 | tr -d '.[:space:]'");
                    p.StandardInput.WriteLine("exit");
                    output = p.StandardOutput.ReadToEnd().Trim();
                    if (output == "')") deviceImei = "Inaccessible";
                    else deviceImei = output;
                    p.WaitForExitAsync();
                }
                
                

                //get storage
                p.StartInfo.FileName = "./res/platform-tools/adb.exe";
                p.StartInfo.Arguments = "shell";
                p.Start();
                p.StandardInput.WriteLine("df -h | grep '/data'");
                p.StandardInput.WriteLine("exit");
                output = p.StandardOutput.ReadToEnd().Trim();
                output = Regex.Replace(output, @"\s+", " ");
                data = output.Split(" ").ToArray();
                try
                {
                    deviceStorage = data[1] + 'B';
                    deviceStorage = new string(deviceStorage.Where(Char.IsDigit).ToArray());
                    if (int.Parse(deviceStorage) <= 32)
                    {
                        deviceStorage = "32GB";
                    }
                    else if (int.Parse(deviceStorage) <= 64 && int.Parse(deviceStorage)>32)
                    {
                        deviceStorage = "64GB";
                    }
                    else if (int.Parse(deviceStorage) <= 128 && int.Parse(deviceStorage) > 64)
                    {
                        deviceStorage = "128GB";
                    }
                    else if (int.Parse(deviceStorage) <= 256 && int.Parse(deviceStorage) > 128)
                    {
                        deviceStorage = "256GB";
                    }

                }
                catch (IndexOutOfRangeException)
                {
                    deviceStorage = "Can't detect storage.";
                }
                p.WaitForExitAsync();


                //get boot slot
                if (deviceCurrentMode!="Bootloader")
                {
                    p.StartInfo.FileName = "./res/platform-tools/adb.exe";
                    p.StartInfo.Arguments = "shell /bin/getprop ro.boot.slot_suffix";
                    p.Start();
                    output = p.StandardOutput.ReadToEnd().Trim().Replace("_", "");
                    deviceCurrentSlot = output;
                    p.WaitForExitAsync();
                }
                else
                {
                    p.StartInfo.FileName = "./res/platform-tools/fastboot.exe";
                    p.StartInfo.Arguments = "getvar current-slot";
                    p.Start();
                    output = p.StandardError.ReadToEnd().Trim().Replace("_", "");
                    deviceCurrentSlot = output;
                    p.WaitForExitAsync();
                }
                

            }
            else
            {
                p.StartInfo.FileName = "./res/platform-tools/fastboot.exe";
                p.StartInfo.Arguments = "getvar product";
                p.Start();
                output = p.StandardError.ReadLine().Trim();
                data = output.Split(" ").ToArray();
                deviceModel = data[1];
                deviceName = "Bootloader Mode";
                p.WaitForExitAsync();

                //get slot in fastboot
                p.StartInfo.FileName = "./res/platform-tools/fastboot.exe";
                p.StartInfo.Arguments = "getvar current-slot";
                p.Start();
                output = p.StandardError.ReadToEnd().Trim().Replace("_", "");
                data = output.Replace("\r"," ").Replace("\n","").Split(" ").ToArray();
                
                deviceCurrentSlot = data[1];
                p.WaitForExitAsync();
            }

            
            
            p.WaitForExitAsync();
            p.Kill();
            b.Content = "Refresh";

        }

        string GetProp(string prop) {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "./res/platform-tools/adb.exe";
            p.StartInfo.Arguments = "shell getprop " + prop;
            p.Start();
            var ret = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExitAsync();
            return ret;
        }


    }
}
