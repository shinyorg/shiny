# Descriptors

Descriptors have minimal functionality.  You can read & write their values. 

_It is important (like services and characteristics) that you do not maintain an instance to descriptors across connections_

**Read/Write to a Descriptor**
```csharp
// once you have your characteristic instance from the characteristic
await descriptor.Write(bytes);

await descriptor.Read();
```

**Monitor Descriptor Read/Writes**
```csharp

descriptor.WhenRead().Subscribe(x => {});


descriptor.WhenWritten().Subscribe(x => {});

```