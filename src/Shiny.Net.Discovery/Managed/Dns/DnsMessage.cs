namespace Shiny.Net.Discovery.Managed.Dns;


/// <summary>
/// A DNS message. mDNS uses the same wire format as unicast DNS with a few bits reinterpreted
/// (see <see cref="DnsQuestion.UnicastResponse"/> and <see cref="DnsResourceRecord.CacheFlush"/>).
/// </summary>
sealed class DnsMessage
{
    const ushort ResponseFlag = 0x8400; // QR=1, AA=1 - mDNS responses are always authoritative

    /// <summary>Transaction id. mDNS ignores it, so it is normally zero.</summary>
    public ushort Id { get; init; }

    public ushort Flags { get; init; }

    public bool IsResponse => (this.Flags & 0x8000) != 0;

    public List<DnsQuestion> Questions { get; } = new();

    public List<DnsResourceRecord> Answers { get; } = new();

    public List<DnsResourceRecord> Authorities { get; } = new();

    public List<DnsResourceRecord> Additionals { get; } = new();

    /// <summary>
    /// Answers plus additionals - the records worth feeding into a cache.
    /// </summary>
    /// <remarks>
    /// Authority records are deliberately excluded. In an mDNS query that section holds a prober's
    /// *proposed* records (RFC 6762 section 8.2), which are tentative by definition - caching them
    /// makes a publisher that is about to be renamed briefly appear under the name it is losing.
    /// </remarks>
    public IEnumerable<DnsResourceRecord> CacheableRecords
        => this.Answers.Concat(this.Additionals);


    /// <summary>
    /// mDNS ignores the transaction id, which makes it a convenient marker for recognising our
    /// own packets coming back through multicast loopback.
    /// </summary>
    public static DnsMessage CreateQuery(ushort id = 0) => new() { Id = id };

    public static DnsMessage CreateResponse() => new() { Flags = ResponseFlag };


    public byte[] ToArray()
    {
        var writer = new DnsWriter();
        writer.WriteUInt16(this.Id);
        writer.WriteUInt16(this.Flags);
        writer.WriteUInt16((ushort)this.Questions.Count);
        writer.WriteUInt16((ushort)this.Answers.Count);
        writer.WriteUInt16((ushort)this.Authorities.Count);
        writer.WriteUInt16((ushort)this.Additionals.Count);

        foreach (var question in this.Questions)
            question.Write(writer);

        foreach (var record in this.Answers)
            record.Write(writer);

        foreach (var record in this.Authorities)
            record.Write(writer);

        foreach (var record in this.Additionals)
            record.Write(writer);

        return writer.ToArray();
    }


    /// <summary>
    /// Parses a received packet.
    /// </summary>
    /// <exception cref="DnsFormatException">The packet is truncated or malformed.</exception>
    public static DnsMessage Parse(byte[] data)
    {
        var reader = new DnsReader(data);
        var id = reader.ReadUInt16();
        var flags = reader.ReadUInt16();
        var questionCount = reader.ReadUInt16();
        var answerCount = reader.ReadUInt16();
        var authorityCount = reader.ReadUInt16();
        var additionalCount = reader.ReadUInt16();

        var message = new DnsMessage { Id = id, Flags = flags };

        for (var i = 0; i < questionCount; i++)
            message.Questions.Add(DnsQuestion.Read(reader));

        ReadRecords(reader, answerCount, message.Answers);
        ReadRecords(reader, authorityCount, message.Authorities);
        ReadRecords(reader, additionalCount, message.Additionals);

        return message;
    }


    static void ReadRecords(DnsReader reader, int count, List<DnsResourceRecord> target)
    {
        for (var i = 0; i < count; i++)
        {
            // a packet can lie about its counts; stop cleanly rather than throwing away
            // the records we did parse
            if (!reader.HasRemaining)
                break;

            target.Add(DnsResourceRecord.Read(reader));
        }
    }


    public override string ToString()
        => $"{(this.IsResponse ? "response" : "query")} q={this.Questions.Count} an={this.Answers.Count} ad={this.Additionals.Count}";
}
