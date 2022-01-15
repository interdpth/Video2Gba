using System;
using System.IO;
using System.Security.Cryptography;

namespace Video2Gba
{
    public class Container
    {
        //either we have a video

        //or we have an index. 

        private byte[] _data;

        private byte[] _ogdata;
        private long _sharedIndex;

        public long ID { get { return _id; } }

        private long _id;

        public string CheckSum;

        public byte[] OGData { get { return _ogdata; } set { if (_changeChange) { _ogdata = value; } else throw new Exception("No change flag"); } }

        private bool _changeChange;

        //Only for cloning.
        public Container(Container src)
        {
            _ogdata = src.OGData;
            SetData(src.Data);
            _sharedIndex = src.Index;
            _id = src.ID;
            CheckSum = src.CheckSum;
        }

        public Container(long id, byte[] data = null, int theIndex = -1, bool allowOgChanges = false)
        {
            _changeChange = allowOgChanges;
            _ogdata = data;
            SetData(data);
            _sharedIndex = theIndex;
            _id = id;
        }

        public Container(long id, short[] data = null, int theIndex = -1)
        {
            byte[] newData = new byte[Length * 2];
            Buffer.BlockCopy(data, 0, newData, 0, (int)Length * 2);
            _ogdata = newData;
            SetData(newData);
            _sharedIndex = theIndex;
            _id = id;
        }

        //If not data, returns 0
        public long Length { get { return IsData() ? _data.Length : 0; } }

        public byte[] Data { get { return _data; } }

        public long Index { get { return _sharedIndex; } }
        public bool IsData() { return _data != null; }

        public void SetIndex(long index)
        {
            if (_id == index)
            {
                throw new Exception("Invalid operation");
            }
            _data = null;
            _ogdata = null;//Duplicate frame, we don't need any data.
            _sharedIndex = index;
        }


        public void SetData(byte[] dat)
        {
            //if(dat.Length  %2 != 0)
            //{
            //    throw new Exception("uh oh");
            //}
            _sharedIndex = -1;
            _data = dat;
            GetCheckSum();
        }

        //We checksum against OG data.
        public void GetCheckSum()
        {
            try
            {
                using (var md5Instance = MD5.Create())
                {
                    using (var stream = new MemoryStream(_ogdata))
                    {
                        var hashResult = md5Instance.ComputeHash(stream);

                        CheckSum = BitConverter.ToString(hashResult).Replace("-", "").ToLowerInvariant();

                        return;
                    }
                }
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
}
