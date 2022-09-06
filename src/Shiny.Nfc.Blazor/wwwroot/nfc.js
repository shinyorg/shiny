export async function read(dotNetRef) {
    const reader = new NDEFReader();

    var promise = new Promise((resolve, reject) => {
        reader.addEventListener('readingerror', e => reject(e));
        reader.addEventListener("reading", ({ message, serialNumber }) => {
//            log(`> Serial Number: ${serialNumber}`);
//            log(`> Records: (${message.records.length})`);

        });
        reader.scan();
    });
    var reading = await promise;
    reader = null;

    return reading;
}

export async function write(msg, readOnly) {
    const reader = new NDEFReader();
    await ndef.write(msg);

    if (readOnly)
        await ndef.makeReadOnly();

    reader = null;
}