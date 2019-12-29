using System;


namespace Shiny.Nfc
{
    public interface INfcDelegate
    {
        void OnReceived(INDefRecord[] records);
    }
}

///// <summary>
///// Class describing the information containing within a NFC tag
///// </summary>
//public class NFCNdefRecord
//{
//	/// <summary>
//	/// NDEF Type
//	/// </summary>
//	public NFCNdefTypeFormat TypeFormat { get; set; }

//	/// <summary>
//	/// MimeType used for <see cref="NFCNdefTypeFormat.Mime"/> type
//	/// </summary>
//	public string MimeType { get; set; } = "text/plain";

//	/// <summary>
//	/// External domain used for <see cref="NFCNdefTypeFormat.External"/> type
//	/// </summary>
//	public string ExternalDomain { get; set; }

//	/// <summary>
//	/// External type used for <see cref="NFCNdefTypeFormat.External"/> type
//	/// </summary>
//	public string ExternalType { get; set; }

//	/// <summary>
//	/// Payload
//	/// </summary>
//	public byte[] Payload { get; set; }

//	/// <summary>
//	/// Uri
//	/// </summary>
//	public string Uri { get; set; }

//	/// <summary>
//	/// String formatted payload
//	/// </summary>
//	public string Message => NFCUtils.GetMessage(TypeFormat, Payload, Uri);
//}

///// <summary>
///// Enumeration of NDEF type
///// </summary>
//public enum NFCNdefTypeFormat
//{
//	Empty = 0x00,
//	WellKnown = 0x01,
//	Mime = 0x02,
//	Uri = 0x03,
//	External = 0x04,
//	Unknown = 0x05,
//	Unchanged = 0x06,
//	Reserved = 0x07
//}