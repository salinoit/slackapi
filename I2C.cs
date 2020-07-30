using System.Runtime.InteropServices;

namespace consoleApp
{
    public class I2C
    {
        private static int OPEN_READ_WRITE = 2;        
        private enum IOCTL_COMMAND
        {

            // número de vezes que um endereço de dispositivo deve ser consultado quando não se reconhece
            I2C_RETRIES = 0x0701,

            // definir tempo limite em unidades de 10 ms
            I2C_TIMEOUT = 0x0702,

            // Use this slave address 
            I2C_SLAVE = 0x0703,

            // 0 p/ 7 bits end., != 0 p 10 bits 
            I2C_TENBIT = 0x0704,

            // Obtenha a máscara de funcionalidade do adaptador
            I2C_FUNCS = 0x0705,

            // Use este endereço slave, mesmo que já esteja sendo usado por um driver!
            I2C_SLAVE_FORCE = 0x0706,

            // R/W (one STOP only) 
            I2C_RDWR = 0x0707,

            // != 0 p/ PEC com SMBus 
            I2C_PEC = 0x0708,

            // SMBus transfereridor 
            I2C_SMBUS = 0x0720,   
        }

        [DllImport("libc.so.6", EntryPoint = "open")]
        public static extern int Open(string fileName, int mode);

        [DllImport("libc.so.6", EntryPoint = "close")]
        public static extern int Close(int handle);

        [DllImport("libc.so.6", EntryPoint = "ioctl", SetLastError = true)]
        private extern static int Ioctl(int handle, int request, int data);

        [DllImport("libc.so.6", EntryPoint = "read", SetLastError = true)]
        internal static extern int Read(int handle, byte[] data, int length);

        [DllImport("libc.so.6", EntryPoint = "write", SetLastError = true)]
        internal static extern int Write(int handle, byte[] data, int length);

        private int handle = -1;

        public void OpenDevice(string file, int address)
        {           
            handle = Open("/dev/i2c-1", OPEN_READ_WRITE);
            var deviceReturnCode = Ioctl(handle, (int)IOCTL_COMMAND.I2C_SLAVE, address);
        }

        public void CloseDevice()
        {
            Close(handle);
            handle = -1;
        }

        protected void WriteByte(byte data)
        {
            byte[] bdata = new byte[] { data };
            Write(handle, bdata, bdata.Length);
        }
    }
}
