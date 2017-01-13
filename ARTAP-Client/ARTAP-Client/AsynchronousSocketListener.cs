﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ARTAPclient
{
    /// <summary>
    /// Asynchronously handle network connection to
    /// the server. In this case, the HoloLens.
    /// </summary>
    public class AsynchronousSocketListener
    {

        #region Fields

        /// <summary>
        /// IPEndPoint describing the host we are connecting to
        /// </summary>
        private readonly IPEndPoint _remoteEndPoint;

        /// <summary>
        /// The socket for the connection to take place on
        /// </summary>
        private Socket _client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

        /// <summary>
        /// Types of messages that can be sent and their
        /// coresponding message  integer codes
        /// </summary>
        private enum MessageType { Bitmap = 1 }

        /// <summary>
        /// Handles timing for checking if the connection is alive
        /// </summary>
        private System.Timers.Timer _connectionAliveTimer;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for Asynch Socket Listener
        /// </summary>
        /// <param name="remoteEndPoint">Remote end point to connect to</param>
        public AsynchronousSocketListener(IPEndPoint remoteEndPoint)
        {
            _remoteEndPoint = remoteEndPoint;
            _connectionAliveTimer = new System.Timers.Timer(5000);
            _connectionAliveTimer.Elapsed += ConnectionAliveTimerElapsed;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Connect to the server (HoloLens)
        /// </summary>
        public void Connect()
        {
            // Connect to a remote device.
            try
            {
                // Connect to the remote endpoint.
                _client.BeginConnect(_remoteEndPoint,
                    new AsyncCallback(ConnectCallback), _client);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Close socket connection gracefully
        /// </summary>
        public void CloseConnection()
        {
            _connectionAliveTimer.Stop();
            _client.Close();
        }

        /// <summary>
        /// Send bitmap to the server (HoloLens)
        /// </summary>
        /// <param name="bm">Bitmap object to send</param>
        public void SendBitmap(ImageSource image)
        {
            BitmapSource bmpSrc = image as BitmapSource;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            BitmapFrame outputFrame = BitmapFrame.Create(bmpSrc);
            encoder.Frames.Add(outputFrame);
            encoder.QualityLevel = 100;

            byte[] imgData;
            using (var imgStream = new MemoryStream())
            {
                encoder.Save(imgStream);
                imgStream.Position = 0;
                imgData = imgStream.ToArray();
            }
            Send(MessageType.Bitmap, imgData);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Combines byte arrays passed in, code borrowed from:
        /// http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
        /// </summary>
        /// <param name="arrays">Arrays passed in to combine</param>
        /// <returns>A single combined byte array</returns>
        private byte[] CombineArrs(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        /// <summary>
        /// Callback method for the connection taking place
        /// </summary>
        /// <param name="ar">IAsyncResult parameter</param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndConnect(ar);
                Connected = true;
                ConnectionEstablished?.Invoke(this, new EventArgs());
            }
            catch(System.Net.Sockets.SocketException e)
            {
                ConnectionTimedOut?.Invoke(this, new EventArgs());
            }
            ///
            /// Start polling to know the connection is alive
            ///
            _connectionAliveTimer.Start();
        }

        /// <summary>
        /// Sends message to the server (HoloLens)
        /// </summary>
        /// <param name="messageType">Type of message being sent</param>
        /// <param name="data">Message data to be sent</param>
        private void Send(MessageType messageType, byte[] data)
        {
            if (Connected)
            {
                ///
                /// Overall message length, +2 for length of message type
                /// 
                int totalLen = data.Length + 2;

                byte[] typeBytes = BitConverter.GetBytes((short)messageType);
                byte[] lengthBytes = BitConverter.GetBytes((int)totalLen);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(typeBytes);
                    Array.Reverse(lengthBytes);
                }

                byte[] combinedData = CombineArrs(lengthBytes, typeBytes, data);
                try
                {
                    _client.BeginSend(combinedData, 0, combinedData.Length, SocketFlags.None,
                        new AsyncCallback(SendCallback), _client);
                }
                catch
                {
                    NotifyConnectionLost();
                }
            }
        }

        /// <summary>
        /// Callback for send completed
        /// </summary>
        /// <param name="ar">IAsyncResult parameter</param>
        private void SendCallback(IAsyncResult ar)
        {
            int bytesSent = _client.EndSend(ar);
            ///
            /// For testing purposes
            ///
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);
        }

        /// <summary>
        /// Fires every time the connection alive poll timer fires
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectionAliveTimerElapsed(object sender, 
            System.Timers.ElapsedEventArgs e)
        {
            ///
            /// Returns true if the closed, reset, etc.
            ///
            bool part1 = _client.Poll(1000, SelectMode.SelectRead);
            ///
            ///Checks if there is anything available to read
            ///
            bool part2 = (_client.Available == 0);
            ///
            /// If it's closed/reset and there is nothing to read,
            /// the socket is not connected and is closed
            /// 
            if (part1 && part2)
            {
                NotifyConnectionLost();
            }
            else
            {
                Connected = true;
            }
        }

        private void NotifyConnectionLost()
        {
            Connected = false;
            _connectionAliveTimer.Stop();
            ConnectionClosed?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Is the socket connected?
        /// </summary>
        public bool Connected { get; private set; }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Connection to server established
        /// </summary>
        public event EventHandler ConnectionEstablished;

        /// <summary>
        /// Connection to server closed or lost
        /// </summary>
        public event EventHandler ConnectionClosed;

        /// <summary>
        /// Connection timed out during connection attempt
        /// </summary>
        public event EventHandler ConnectionTimedOut;

        #endregion

    }
}