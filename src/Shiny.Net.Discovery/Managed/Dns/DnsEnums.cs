namespace Shiny.Net.Discovery.Managed.Dns;


enum DnsRecordType : ushort
{
    A = 1,
    PTR = 12,
    TXT = 16,
    AAAA = 28,
    SRV = 33,
    NSEC = 47,
    ANY = 255
}


enum DnsRecordClass : ushort
{
    IN = 1,
    ANY = 255
}


/// <summary>
/// Thrown when a received packet cannot be parsed. Callers drop the packet rather than failing
/// the whole browse - anything can be on the multicast group.
/// </summary>
sealed class DnsFormatException(string message) : Exception(message);
