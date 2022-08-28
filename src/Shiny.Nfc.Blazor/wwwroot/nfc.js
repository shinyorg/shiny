const reader = new NDEFReader();

export function read(dotNetRef) {
    var promise = new Promise(
        () => { },
        reason => { }
    );

    const onRead = () => {

//        ndef.addEventListener("reading", ({ message, serialNumber }) => {
//            log(`> Serial Number: ${serialNumber}`);
//            log(`> Records: (${message.records.length})`);
//        });
    };
    reader.addEventListener("readingerror", null);
    reader.addEventListener("reading", null);
    reader.scan();
}

export function write() {
    //        const ndef = new NDEFReader();
    //        await ndef.write("Hello world!");
    //await ndef.makeReadOnly();
}