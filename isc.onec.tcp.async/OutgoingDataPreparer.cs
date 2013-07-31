﻿using System;
using System.Net.Sockets;
using isc.general;
using isc.onec.bridge;
using NLog;

namespace isc.onec.tcp.async
{
	class OutgoingDataPreparer
	{
		
		private DataHolder theDataHolder;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		public OutgoingDataPreparer()
		{			
		}

		internal void PrepareOutgoingData(SocketAsyncEventArgs e, DataHolder handledDataHolder)
		{
			DataHoldingUserToken theUserToken = (DataHoldingUserToken)e.UserToken;
		   
			
			theDataHolder = handledDataHolder;

			//In this example code, we will send back the receivedTransMissionId,
			// followed by the
			//message that the client sent to the server. And we must
			//prefix it with the length of the message. So we put 3 
			//things into the array.
			// 1) prefix,
			// 2) receivedTransMissionId,
			// 3) the message that we received from the client, which
			// we stored in our DataHolder until we needed it.
			//That is our communication protocol. The client must know the protocol.

			//Convert the receivedTransMissionId to byte array.
			//Byte[] idByteArray = BitConverter.GetBytes(theDataHolder.receivedTransMissionId);

			//Determine the length of all the data that we will send back.
			//Int32 lengthOfCurrentOutgoingMessage = idByteArray.Length + theDataHolder.dataMessageReceived.Length;
			Byte[] reply;
			if (theDataHolder.isError)
			{
				reply = sendError(theUserToken.getServer());
			}
			else
			{
				reply = process(theUserToken.getServer(), theDataHolder.dataMessageReceived);
			}
			Int32 lengthOfCurrentOutgoingMessage = reply.Length;

			//So, now we convert the length integer into a byte array.
			Byte[] arrayOfBytesInPrefix = BitConverter.GetBytes(lengthOfCurrentOutgoingMessage);
			
			//Create the byte array to send.
			theUserToken.dataToSend = new Byte[theUserToken.sendPrefixLength + lengthOfCurrentOutgoingMessage];
			
			//Now copy the 3 things to the theUserToken.dataToSend.
			Buffer.BlockCopy(arrayOfBytesInPrefix, 0, theUserToken.dataToSend, 0, theUserToken.sendPrefixLength);
			//Buffer.BlockCopy(idByteArray, 0, theUserToken.dataToSend, theUserToken.sendPrefixLength, idByteArray.Length);
			//The message that the client sent is already in a byte array, in DataHolder.
			Buffer.BlockCopy(reply, 0, theUserToken.dataToSend, theUserToken.sendPrefixLength , reply.Length);
			
			theUserToken.sendBytesRemainingCount = theUserToken.sendPrefixLength + lengthOfCurrentOutgoingMessage;
			theUserToken.bytesSentAlreadyCount = 0;
		}

		private byte[] sendError(Server server)
		{
			string[] reply;
			try
			{
				RequestMessage request = RequestMessage.createDisconnectMessage();
				server.run(request.command, request.target, request.operand, request.vals, request.types);
				logger.Error("OutgoingDataPreparer.sendError(): error was sent");

				reply = new string[] { ((int)Response.Type.EXCEPTION).ToString(), "Bridge: Fatal network error" };
			}
			catch (Exception e)
			{
				reply = new string[] { ((int)Response.Type.EXCEPTION).ToString(), "Bridge: Fatal network error. Unprocessed exception." };
				logger.ErrorException("Unprocessed exception:",e);
			}

			return new MessageEncoder(reply).encode();
		}

		private static byte[] process(Server server,byte[] data) {
			string[] reply;
			try {
				RequestMessage request = (new MessageDecoder(data)).decode();

				//TODO Debug why?
				if (server == null) {
					//throw new Exception("OutgoingDataPreparer.process(): no server object");
					logger.Error("OutgoingDataPreparer.process(): no server object.");
					reply = new string[] { Convert.ToInt32(Response.Type.EXCEPTION).ToString(), "OutgoingDataPreparer.process(): no server object" };

				} else {
					reply = server.run(request.command, request.target, request.operand, request.vals, request.types);
					//logger.Debug("reply:" + reply[0] + "," + reply[1]);
				}
			} catch (Exception e) {
				reply = new string[] {
					Convert.ToInt32(Response.Type.EXCEPTION).ToString(),
					e.ToStringWithIlOffsets()
				};
			}
			return new MessageEncoder(reply).encode();
		}
	}
}
