using System.Text;
using System.Threading;

namespace consoleApp
{
    public class Lcd1602 : I2C
    {
      
        protected enum Commands
        {
            LCD_CLEARDISPLAY = 0x01,
            LCD_RETURNHOME = 0x02,
            LCD_ENTRYMODESET = 0x04,
            LCD_DISPLAYCONTROL = 0x08,
            LCD_CURSORSHIFT = 0x10,
            LCD_FUNCTIONSET = 0x20,
            LCD_SETCGRAMADDR = 0x40,
            LCD_SETDDRAMADDR = 0x80,
        }
      
        protected enum DisplayEntryMode
        {
            LCD_ENTRYRIGHT = 0x00,
            LCD_ENTRYLEFT = 0x02,
            LCD_ENTRYSHIFTINCREMENT = 0x01,
            LCD_ENTRYSHIFTDECREMENT = 0x00,
        }

        protected enum DisplayControl
        {
            LCD_DISPLAYON = 0x04,
            LCD_DISPLAYOFF = 0x00,
            LCD_CURSORON = 0x02,
            LCD_CURSOROFF = 0x00,
            LCD_BLINKON = 0x01,
            LCD_BLINKOFF = 0x00,
        }

        protected enum DisplayCursorShift
        {
            LCD_DISPLAYMOVE = 0x08,
            LCD_CURSORMOVE = 0x00,
            LCD_MOVERIGHT = 0x04,
            LCD_MOVELEFT = 0x00,
        }

        protected enum FunctionSet
        {
            LCD_8BITMODE = 0x10,
            LCD_4BITMODE = 0x00,
            LCD_2LINE = 0x08,
            LCD_1LINE = 0x00,
            LCD_5x10DOTS = 0x04,
            LCD_5x8DOTS = 0x00,
        }

        protected enum BacklightControl
        {
            LCD_BACKLIGHT = 0x08,
            LCD_NOBACKLIGHT = 0x00,
        }

        protected enum ControlBits
        {
            En = 0x04,  
            Rw = 0x02,  
            Rs = 0x01   
        }

        protected void SendCommand(int comm, BacklightControl backlight = BacklightControl.LCD_BACKLIGHT)
        {
            byte buf;
            buf = (byte)(comm & 0xF0);
            buf |= (byte)((byte)ControlBits.En | (byte)backlight);
            WriteByte(buf);
            Thread.Sleep(2);
            WriteByte((byte)backlight);

            buf = (byte)((comm & 0x0F) << 4);
            buf |= (byte)((byte)ControlBits.En | (byte)backlight);
            WriteByte(buf);
            Thread.Sleep(2);
            WriteByte((byte)backlight);
        }

        protected void SendData(int data, BacklightControl backlight = BacklightControl.LCD_BACKLIGHT)
        {
            byte buf;
            buf = (byte)(data & 0xF0);
            buf |= (byte)ControlBits.En | (byte)BacklightControl.LCD_BACKLIGHT | (byte)ControlBits.Rs;
            buf |= 0x08;
            WriteByte(buf);
            Thread.Sleep(2);
            WriteByte((byte)backlight);

            buf = (byte)((data & 0x0F) << 4);
            buf |= (byte)ControlBits.En | (byte)BacklightControl.LCD_BACKLIGHT | (byte)ControlBits.Rs;
            WriteByte(buf);
            Thread.Sleep(2);
            WriteByte((byte)backlight);
        }

        public void Init()
        {
            SendCommand(0x33); 
            Thread.Sleep(2);
            SendCommand(0x32); 
            Thread.Sleep(2);
            SendCommand(0x28); 
            Thread.Sleep(2);
            SendCommand(0x0C); 
            Thread.Sleep(2);
            SendCommand(0x01); 
        }

        public void Clear()
        {
            SendCommand(0x01); 
        }

        public void ClearAndOff()
        {
            SendCommand((byte)Commands.LCD_CLEARDISPLAY, 0);
        }

        public void DisplayOff()
        {
            SendCommand((byte)Commands.LCD_DISPLAYCONTROL | (byte)DisplayControl.LCD_DISPLAYOFF, 0);
        }

        public void DisplayOn()
        {
            SendCommand((byte)Commands.LCD_DISPLAYCONTROL | (byte)DisplayControl.LCD_DISPLAYON);
        }

        public void Write(int x, int y, string str)
        {            
            int addr = 0x80 + 0x40 * y + x;
            SendCommand(addr);

            byte[] charData = Encoding.ASCII.GetBytes(str);

            foreach (byte b in charData)
            {
                SendData(b);
            }
        }
    }
}
