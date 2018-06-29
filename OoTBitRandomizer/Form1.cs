using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TwitchLib;
using TwitchLib.Events.Client;
using TwitchLib.Models.Client;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace OoTBitRaceRandomizer
{
    public partial class Form1 : Form
    {
        private TwitchClient client = null;
        private Thread ConstantSetThread = null;
        private bool ConstantSetThreadEnabled = false;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        private byte[] TunicColor = new byte[3] { 0x1E, 0x69, 0x1B }; // RGB
        private byte[] OcarinaSound = new byte[1] { 1 }; // 1 - 7


        private void SetConstantValues()
        {
            if (baseAddress != 0 && processHandle != null)
            {
                while (ConstantSetThreadEnabled)
                {
                    WriteCode(OcarinaSound, 1, 0x10220C);
                    WriteCode(TunicColor, TunicColor.Length, 0x0F7AD8);
                    Thread.Sleep(10);
                }
            }
        }


        public Form1()
        {
            InitializeComponent();
        }

        private void SetStatus(string Message)
        {
            StatusLabel.Text = Message;
        }

        public bool enableMessageGet;
        public IntPtr processHandle;
        public int baseAddress = 0;

        private int WriteCode(dynamic NewDataValue, int DataSize, int MemoryOffset)
        {
            if (DataSize == 4)
            {
                NewDataValue = NewDataValue.ToN64();
            }

            byte[] WriteBuffer = null;

            if (NewDataValue.GetType() == typeof(byte))
            {
                WriteBuffer = new byte[1] { NewDataValue };
            }
            else if (NewDataValue.GetType() == typeof(byte[]))
            {
                var Data = (byte[])NewDataValue;
                WriteBuffer = new byte[Data.Length];
                Array.Copy(Data, WriteBuffer, WriteBuffer.Length);
            }
            else
            {
                WriteBuffer = BitConverter.GetBytes(NewDataValue);
            }

            if (WriteBuffer.Length > 2)
            {
                Array.Reverse(WriteBuffer);
            }

            int BytesWritten = 0;
            int Offset = MemoryOffset;
            
            // Reverse Memory Write Offset
            if (WriteBuffer.Length < 4)
            {
                Offset = ((Offset & ~3) | (3 - Offset & 3)) - (WriteBuffer.Length - 1);
            }

            WriteProcessMemory((int)processHandle, baseAddress + Offset, WriteBuffer, WriteBuffer.Length, ref BytesWritten);
            return BytesWritten;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (client == null)
            {
                try
                {
                    ConnectionCredentials credentials = new ConnectionCredentials(textBox1.Text, textBox2.Text);
                    client = new TwitchClient(credentials, textBox3.Text.ToLower());

                    client.OnMessageReceived += onMessageReceived;
                    client.Connect();
                    client.SendMessage("OoT Bit Randomizer Race Bot is running!");

                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    textBox3.Enabled = false;

                    button1.Enabled = false;
                    button4.Enabled = false;

                    SetStatus("Successfully connected to Twitch Account: " + client.TwitchUsername);
                }
                catch
                {
                    MessageBox.Show("There was an error connecting to your Twitch Chat!\r\nPlease check your login credentials.",
                        "Twitch Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void onMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.StartsWith("!bitrando"))
            {
                SendWhisper(e.ChatMessage.Username, "This is an OoT Bit Randomizer run! This means that for donating bits, something in the game will change! " + //client.SendMessage(
                    "A spreadsheet containing the donation values and commands for subscribers can be found here: " +
                    "https://docs.google.com/spreadsheets/d/1MfNefzNxM8w9WgtOevkfo038HUUoKL8vP2z6cDSbJZA/edit?usp=sharing");
            }
            else if (baseAddress != 0)
            {
                CodeInfo SelectedCode = null;
                if ((e.ChatMessage.IsSubscriber || e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster) && e.ChatMessage.Message.StartsWith("!"))
                {
                    string Command = Regex.Match(e.ChatMessage.Message.Substring(1), @"\w+").Value;
                    var ReturnedTuple = BitDonationManager.GetRequestedCode(Command, e.ChatMessage.Bits);
                    SelectedCode = ReturnedTuple.Item1;
                    int ResultCode = ReturnedTuple.Item2;

                    if (ResultCode == 1)
                    {
                        SendWhisper(e.ChatMessage.Username, "Your bit donation has been accepted and the code was run!");
                    }
                    else if (ResultCode == -1)
                    {
                        int BitsRequired = BitDonationManager.GetBitsRequiredForCodeByCommandName(Command);
                        SendWhisper(e.ChatMessage.Username, string.Format("The code you requested required a bit donation of {0} bit{1}. " +
                            "You only donated {2} bit{3}, so a random code was run at that bit value.", BitsRequired, BitsRequired == 1 ? "" : "s",
                            e.ChatMessage.Bits, e.ChatMessage.Bits == 1 ? "" : "s"));
                    }
                    else
                    {
                        SendWhisper(e.ChatMessage.Username, string.Format("A code whose command is {0} was not found! A random code with the same" +
                            " bit donation value was run instead!", Command));
                    }
                }
                else
                {
                    if (e.ChatMessage.Message.StartsWith("!"))
                    {
                        SendWhisper(e.ChatMessage.Username, "You must be a subscriber to pick the code to run! A random code was run for you instead.");
                    }

                    SelectedCode = BitDonationManager.GetRandomCodeInfoForBitAmount(e.ChatMessage.Bits);
                }

                if (SelectedCode != null)
                {
                    if (SelectedCode.Name.Equals("Tunic Color"))
                    {
                        string WhisperMsg = "Code Ran: Tunic Color | Value set to: ";
                        for (int i = 0; i < 3; i++)
                        {
                            dynamic NewDataValue = BitDonationManager.BitsToModifierPower(0, SelectedCode.MinValue, SelectedCode.MaxValue);
                            TunicColor[i] = (byte)NewDataValue;
                            WhisperMsg += (i == 0 ? "R" : (i == 1 ? "G" : "B")) + ": " + NewDataValue.ToString() + " ";
                        }
                        SendWhisper(e.ChatMessage.Username, WhisperMsg);
                    }
                    else if (SelectedCode.Name.Equals("Ocarina Sound"))
                    {
                        dynamic NewDataValue = BitDonationManager.BitsToModifierPower(0, SelectedCode.MinValue, SelectedCode.MaxValue);
                        OcarinaSound = new byte[1] { NewDataValue };
                        SendWhisper(e.ChatMessage.Username, string.Format("Code ran: {0} | Value set to: 0x{1}", SelectedCode.Name, NewDataValue.ToString("X")));
                    }
                    else
                    {
                        dynamic NewDataValue = BitDonationManager.BitsToModifierPower(0, SelectedCode.MinValue, SelectedCode.MaxValue);

                        WriteCode(NewDataValue, SelectedCode.DataSize, SelectedCode.MemoryOffset);
                        SendWhisper(e.ChatMessage.Username, string.Format("Code ran: {0} | Value set to: 0x{1}", SelectedCode.Name, NewDataValue.ToString("X")));
                    }
                }
            }
        }

        private void SendWhisper(string Username, string Message)
        {
            client.SendWhisper(Username, Message);
        }

        public bool enableConnectButton()
            => !string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrEmpty(textBox3.Text);

        private void AuthTextBox_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = enableConnectButton();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Keep in mind that you can always use a bot account to connect to your chat!", "OAuth Notice",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Process.Start("https://twitchapps.com/tmi/");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string ProcessName = "Project64";
            Process[] Processes = Process.GetProcessesByName(ProcessName);

            if (Processes.Length == 0)
            {
                ProcessName = "Project64d";
                Processes = Process.GetProcessesByName(ProcessName);
            }

            if (Processes.Length > 0)
            {
                Process process = Processes[0];
                processHandle = OpenProcess(0x1F0FFF, true, process.Id);

                baseAddress = ReadWritingMemory.GetBaseAddress(ProcessName, 0x10, 4096, 4);

                if (baseAddress != 0)
                {
                    SetStatus("Successfully hooked Project64 & found OoT's RAM Address");
                    try
                    {
                        ConstantSetThreadEnabled = false;
                        if (ConstantSetThread != null)
                        {
                            while (ConstantSetThread.IsAlive) { } // Wait for thread to finish execution
                        }
                        else
                        {
                            ConstantSetThread = new Thread(SetConstantValues);
                        }

                        ConstantSetThreadEnabled = true;
                        ConstantSetThread.Start();
                    }
                    catch { }
                }
                else
                {
                    MessageBox.Show("Failed to find the RAM start address! Please try again at the title screen of OoT!", "Hook Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Failed to find any Project64 processes! Please make sure that Project64 is running.", "Hook Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ConstantSetThreadEnabled = false;

            if (client != null)
            {
                try
                {
                    client.SendMessage("OoT Bit Randomizer Race Bot has stopped!");
                    client.Disconnect();
                }
                catch { }
            }
        }
    }
}
