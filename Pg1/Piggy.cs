using Iot.Device.Bno055;
using Iot.Device.Ft232H;
using Iot.Device.FtCommon;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Pg1
{
    public class Piggy
    {

        private GpioController gpio;
        private bool opening = false;

        public Piggy()
        {
            var devices = FtCommon.GetDevices();
            Console.WriteLine($"{devices.Count} available device(s)");
            foreach (var device in devices)
            {
                Console.WriteLine($"  {device.Description}");
                Console.WriteLine($"    Flags: {device.Flags}");
                Console.WriteLine($"    Id: {device.Id}");
                Console.WriteLine($"    LocId: {device.LocId}");
                Console.WriteLine($"    Serial number: {device.SerialNumber}");
                Console.WriteLine($"    Type: {device.Type}");
            }

            if (devices.Count == 0)
            {
                Console.WriteLine("Error: No GPIO controller connected");
                return;
            }

            var ft232h = new Ft232HDevice(devices[0]);

            gpio = ft232h.CreateGpioController();


            //setup pins
            
            //mouth motor
            gpio.OpenPin(4);
            gpio.SetPinMode(4, PinMode.Output);
            gpio.OpenPin(5);
            gpio.SetPinMode(5, PinMode.Output);
            
            //gpio.OpenPin(6);
            //gpio.SetPinMode(6, PinMode.Output);

            //head motor
            gpio.OpenPin(8);
            gpio.SetPinMode(8, PinMode.Output);
            gpio.OpenPin(9);
            gpio.SetPinMode(9, PinMode.Output);
            
            //gpio.OpenPin(10);
            //gpio.SetPinMode(10, PinMode.Output);

            //LED
            gpio.OpenPin(11);
            gpio.SetPinMode(11, PinMode.Output);
            gpio.Write(11, 0);

            //buzzer
            gpio.OpenPin(15);
            gpio.SetPinMode(15, PinMode.Output);
            
            Speech.VisemeReceived += Speech_VisemeReceived;
            Speech.WordBoundary += Speech_WordBoundary;
        }

        public async Task Say(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Task t = Speech.Speak(message);
            t.Wait();

            Console.WriteLine("Closing mouth");
            //ShowViseme(0);
            bkg1 = new BackgroundWorker();
            bkg1.DoWork += Bkg1_DoWork;
            bkg1.RunWorkerCompleted += Bkg1_RunWorkerCompleted;
            bkg1.RunWorkerAsync();
        }

        private void Speech_WordBoundary(object? sender, SpeechSynthesisWordBoundaryEventArgs e)
        {            
            //Console.Write(e.Text);
            //Console.Write("(" + e.Duration.TotalMilliseconds + ")");
            int cycle = 250;
            int pause = 15;                       

            int q = 0;
            int n = 1;
            if (e.Duration.TotalMilliseconds <= 0) return;
            if (e.Duration.TotalMilliseconds < cycle)
            {
                cycle = (int)e.Duration.TotalMilliseconds;
            }
            else
            {
                Random r = new Random();
                q = r.Next(-cycle / 2, cycle / 2);
                n = (int)Math.Floor(e.Duration.TotalMilliseconds / cycle);
            }

            //Console.Write("-" + n + "-");

            for (int i = 0; i < n; i++)
            {
                cycle -= q;
                if (cycle / 2 <= pause) continue;

                //Console.Write(".");
                MouthOpen();
                Thread.Sleep((cycle / 2) - pause);
                MouthStop();
                Thread.Sleep(pause);

                MouthClose();
                Thread.Sleep((cycle / 2) - pause);

                MouthStop();
                
                Thread.Sleep(pause);
                cycle += q;
                cycle += q;
            }
        }

        public void LightOn()
        {
            gpio.Write(11, 1);
        }

        public void LightOff()
        {
            gpio.Write(11, 0);
        }

        private void Speech_VisemeReceived(object? sender, SpeechSynthesisVisemeEventArgs e)
        {
            //Console.Write(e.VisemeId + "-");
            //ShowViseme(e.VisemeId);
        }

        private DateTime lastVisemeReceived = DateTime.Now;
        private int lastVisemeId = 0;
        BackgroundWorker bkg1 = new BackgroundWorker();
        private void ShowViseme(uint visemeId)
        {
            //if (((DateTime.Now - lastVisemeReceived).TotalMilliseconds < 10) && visemeId != 0) return;

            lastVisemeReceived = DateTime.Now;
            if (lastVisemeId == visemeId ) return;

            bool shouldOpen;
            if ((visemeId == 0) || (visemeId == 10) || (visemeId == 13) || (visemeId == 3) || (visemeId == 7) || (visemeId == 18) || (visemeId == 21))
            {
                shouldOpen = false;
            } 
            else
            {
                shouldOpen = true;
            }

            if (opening && !shouldOpen)
            {
                MouthClose();
            }

            if (!opening && shouldOpen)
            {
                MouthOpen();
            }

            Thread.Sleep(10);

            if (!shouldOpen)
            {
                MouthClose();
                opening = false;
            }
            else
            {                
                MouthOpen();
                opening = true;
            }


         

            bkg1 = new BackgroundWorker();
            bkg1.DoWork += Bkg1_DoWork;
            bkg1.RunWorkerCompleted += Bkg1_RunWorkerCompleted;
            bkg1.RunWorkerAsync();
        }

        private void Bkg1_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            MouthStop();            
        }

        private void Bkg1_DoWork(object? sender, DoWorkEventArgs e)
        {
            Thread.Sleep(100);
        }

        public void HeadLeft()
        {
            gpio.Write(8, 1);
            gpio.Write(9, 0);
            //gpio.Write(10, 1);
        }

        public void HeadRight()
        {
            gpio.Write(8, 0);
            gpio.Write(9, 1);
            //gpio.Write(10, 1);
        }

        public void HeadStop()
        {
            gpio.Write(8, 0);
            gpio.Write(9, 0);
            //gpio.Write(10, 1);
        }


        public void MouthOpen()
        {
            gpio.Write(4, 1);
            gpio.Write(5, 0);
            //gpio.Write(6, 1);
        }

        public void MouthClose()
        {
            gpio.Write(4, 0);
            gpio.Write(5, 1);
            //gpio.Write(6, 1);
        }

        public void MouthStop()
        {
            gpio.Write(4, 0);
            gpio.Write(5, 0);
            //gpio.Write(6, 1);
        }

        private void MouthTalk(int n)
        {

            for (int i = 0; i < n; i++)
            {
                Random r = new Random();
                int pause = r.Next(300) + 100;

                MouthOpen();
                Thread.Sleep(pause);

                MouthClose();
                Thread.Sleep(pause);
            }
            MouthStop();
        }

        BackgroundWorker bkg;
        public void HeadStart()
        {
            HeadLeft();
            bkg = new BackgroundWorker();
            bkg.DoWork += Bkg_DoWork;
            bkg.RunWorkerCompleted += Bkg_RunWorkerCompleted;
            bkg.RunWorkerAsync();
        }

        public void HeadEnd()
        {
            HeadRight();
            bkg = new BackgroundWorker();
            bkg.DoWork += Bkg_DoWork;
            bkg.RunWorkerCompleted += Bkg_RunWorkerCompleted;
            bkg.RunWorkerAsync();
        }

        private void Bkg_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            HeadStop();
            Console.Write("Head Stopped");
        }

        private void Bkg_DoWork(object? sender, DoWorkEventArgs e)
        {
            Thread.Sleep(300);
        }

        public void Beep(int ms)
        {
            gpio.Write(15, 1);
            Thread.Sleep(ms);
            gpio.Write(15, 0);
        }
    }
}
