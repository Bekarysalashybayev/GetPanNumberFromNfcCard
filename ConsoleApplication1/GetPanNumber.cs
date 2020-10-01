using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PCSC;
using PCSC.Iso7816;

namespace ConsoleApplication1
{
    class GetPanNumber
    {
        public static byte[] selectAid = { (byte)0x32, (byte)0x50, (byte)0x041, (byte)0x59, (byte)0x2E, (byte)0x53, (byte)0x59, (byte)0x53, (byte)0x2E, (byte)0x44, (byte)0x44, (byte)0x46, (byte)0x30, (byte)0x31 };
        public static byte[] readMasterRecord ={(byte)0x00, (byte)0xB2, (byte)0x01, (byte)0x14, (byte)0x00};
        public static byte[] selectMCAID = {(byte)0xA0, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x04, (byte)0x10, (byte)0x10};
        public static byte[] selectVisaAID = {(byte)0xA0, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x03, (byte)0x10, (byte)0x10};



        static void Main(String[] args)
        {
            var context = new SCardContext();

            context.Establish(SCardScope.System);

            var readerNames = context.GetReaders();

            if (readerNames == null || readerNames.Length < 1)
            {
                Console.WriteLine("You need at least one reader in order to run this example.");
                Console.ReadKey();
                return;
            }

            var readerName = readerNames[0];
            if (readerName == null)
            {
                return;
            }

            string panNumber = getPanNumber1(context, readerName);

            if (panNumber == null || panNumber.Length < 1)
            {
                Console.WriteLine("error");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Pan Number: {0}", panNumber);
            Console.ReadKey();


        }

        private static string getPanNumber1(SCardContext context, string readerName)
        {
            var rfidReader = new SCardReader(context);

            byte[] readRecord = new byte[0];
            byte x = 0x00;
            var sc = rfidReader.Connect(readerName, SCardShareMode.Shared, SCardProtocol.Any);
            if (sc != SCardError.Success)
            {
                return null;
            }

            //----------------Get AID---------------------------
            var apdu = new CommandApdu(IsoCase.Case4Short, rfidReader.ActiveProtocol)
            {
                CLA = 0x00,
                INS = 0xA4,
                P1 = 0x04,
                P2 = 0x00,
                Le = 0x00,
                Data = selectAid
            };


            sc = rfidReader.BeginTransaction();
            if (sc != SCardError.Success)
            {
                return null;
            }

            var receivePci = new SCardPCI();
            var sendPci = SCardPCI.GetPci(rfidReader.ActiveProtocol);

            var receiveBuffer = new byte[256];
            var command = apdu.ToArray();

            sc = rfidReader.Transmit(
                sendPci,
                command,
                receivePci,
                ref receiveBuffer);

            if (sc != SCardError.Success)
            {
                return null;
            }

            var responseApdu = new ResponseApdu(receiveBuffer, IsoCase.Case2Short, rfidReader.ActiveProtocol);

            if (!responseApdu.HasData)
            {
                return null;
            }

            String aid;
            byte[] aidByte = (byte[])responseApdu.GetData();
            String pAid = ByteArrayToHexString(aidByte);
            int aidInd = pAid.LastIndexOf("4F07");
            aid = pAid.Substring(aidInd + 4, 14);

            //------------------------ end ------------------------------

            if (aid.Equals("A0000000041010"))
            {
                readRecord = selectMCAID;
                x = 0x14;
            }
            else if (aid.Equals("A0000000031010"))
            {
                readRecord = selectVisaAID;
                x = 0x1C;
            }
            //----------------selectMCAID---------------------------

            apdu = new CommandApdu(IsoCase.Case4Short, rfidReader.ActiveProtocol)
            {
                CLA = 0x00,
                INS = 0xA4,
                P1 = 0x04,
                P2 = 0x00,
                Le = 0x00,
                Data = readRecord
            };


            sc = rfidReader.BeginTransaction();
            if (sc != SCardError.Success)
            {
                return null;
            }

            receivePci = new SCardPCI();
            sendPci = SCardPCI.GetPci(rfidReader.ActiveProtocol);

            receiveBuffer = new byte[256];
            command = apdu.ToArray();
            sc = rfidReader.Transmit(
                sendPci,
                command,
                receivePci,
                ref receiveBuffer);

            if (sc != SCardError.Success)
            {
                return null;
            }

            responseApdu = new ResponseApdu(receiveBuffer, IsoCase.Case2Short, rfidReader.ActiveProtocol);

            if (!responseApdu.HasData)
            {
                return null;
            }

            //------------------------ end ------------------------------


            apdu = new CommandApdu(IsoCase.Case2Short, rfidReader.ActiveProtocol)
            {
                CLA = 0x00,
                INS = 0xB2,
                P1 = 0x01,
                P2 = x,
                Le = 0x00
            };


            sc = rfidReader.BeginTransaction();
            if (sc != SCardError.Success)
            {
                return null;
            }

            receivePci = new SCardPCI();
            sendPci = SCardPCI.GetPci(rfidReader.ActiveProtocol);

            receiveBuffer = new byte[256];
            command = apdu.ToArray();

            sc = rfidReader.Transmit(
                sendPci,
                command,
                receivePci,
                ref receiveBuffer);

            if (sc != SCardError.Success)
            {
                return null;
            }

            responseApdu = new ResponseApdu(receiveBuffer, IsoCase.Case2Short, rfidReader.ActiveProtocol);

            if (!responseApdu.HasData)
            {
                return null;
            }

            String pan;
            byte[] a = (byte[])responseApdu.GetData();
            String p = ByteArrayToHexString(a);
            int panInd = p.LastIndexOf("5A08");
            pan = p.Substring(panInd + 4, 16);

            rfidReader.EndTransaction(SCardReaderDisposition.Leave);
            rfidReader.Disconnect(SCardReaderDisposition.Reset);

            return pan;

        }

        private static string ByteArrayToHexString(byte[] inarray)
        {
            int i, j, ii;
            string[] hex = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F" };
            string o = "";
            for (j = 0; j < inarray.Length; ++j)
            {
                ii = (int)inarray[j] & 0xff;
                i = (ii >> 4) & 0x0f;
                o += hex[i];
                i = ii & 0x0f;
                o += hex[i];
            }
            return o;
        }
    }
}
