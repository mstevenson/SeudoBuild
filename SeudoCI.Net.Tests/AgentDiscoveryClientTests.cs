using System;
using System.Net;
using Makaretu.Dns;

namespace SeudoCI.Net.Tests;

[TestFixture]
public class AgentDiscoveryClientTests
{
    [Test]
    public void ProcessServiceAnnouncementRegistersAgent()
    {
        using var client = new AgentDiscoveryClient();
        Agent? discovered = null;
        client.AgentDiscovered += (_, args) => discovered = args.Agent;

        var instanceName = (DomainName)$"SeudoCI-test-agent._seudoci._tcp.local";
        var message = CreateAnnouncement(instanceName, "My Agent", IPAddress.Parse("192.168.0.42"), 5511);

        client.ProcessServiceAnnouncement(message, instanceName);

        Assert.Multiple(() =>
        {
            Assert.That(discovered, Is.Not.Null);
            Assert.That(discovered!.Name, Is.EqualTo("My Agent"));
            Assert.That(discovered.Address, Is.EqualTo("192.168.0.42:5511"));
        });

        var retrieved = client.FindByName("My Agent");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Address, Is.EqualTo("192.168.0.42:5511"));
    }

    [Test]
    public void ProcessServiceAnnouncementUpdatesAgent()
    {
        using var client = new AgentDiscoveryClient();
        Agent? updated = null;
        client.AgentUpdated += (_, args) => updated = args.Agent;

        var instanceName = (DomainName)$"SeudoCI-update-agent._seudoci._tcp.local";
        var initial = CreateAnnouncement(instanceName, "Update Agent", IPAddress.Parse("192.168.0.50"), 5511);
        var changed = CreateAnnouncement(instanceName, "Update Agent", IPAddress.Parse("192.168.0.51"), 5511);

        client.ProcessServiceAnnouncement(initial, instanceName);
        client.ProcessServiceAnnouncement(changed, instanceName);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Address, Is.EqualTo("192.168.0.51:5511"));
    }

    private static Message CreateAnnouncement(DomainName instanceName, string displayName, IPAddress address, ushort port)
    {
        var message = new Message();
        var servicePointer = new PTRRecord
        {
            Name = (DomainName)"_seudoci._tcp.local",
            DomainName = instanceName,
            TTL = TimeSpan.FromMinutes(1)
        };
        message.Answers.Add(servicePointer);

        var hostName = DomainName.Join(instanceName, "seudoci", "local");
        var srv = new SRVRecord
        {
            Name = instanceName,
            Port = port,
            Target = hostName,
            TTL = TimeSpan.FromMinutes(2)
        };
        message.AdditionalRecords.Add(srv);

        var txt = new TXTRecord
        {
            Name = instanceName,
            TTL = TimeSpan.FromMinutes(2)
        };
        txt.Strings.Add("txtvers=1");
        txt.Strings.Add($"name={displayName}");
        message.AdditionalRecords.Add(txt);

        var addressRecord = AddressRecord.Create(hostName, address);
        addressRecord.TTL = TimeSpan.FromMinutes(2);
        message.AdditionalRecords.Add(addressRecord);

        return message;
    }
}
