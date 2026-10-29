// Web NFC (NDEFReader) for Shiny.Nfc. Records cross to .NET as JSON with base64 bytes, so the .NET
// side can use a source-generated serializer and stay trim-safe.

let reader = null;
let controller = null;

const toBase64 = (view) => {
    const bytes = new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
    let binary = "";
    for (let i = 0; i < bytes.length; i += 0x8000)
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
    return btoa(binary);
};

const fromBase64 = (value) => Uint8Array.from(atob(value), c => c.charCodeAt(0));

const toDto = (record) => {
    const dto = { recordType: record.recordType };
    if (record.mediaType) dto.mediaType = record.mediaType;
    if (record.id) dto.id = record.id;
    if (record.encoding) dto.encoding = record.encoding;
    if (record.lang) dto.lang = record.lang;
    if (record.data) dto.data = toBase64(record.data);
    return dto;
};

const fromDto = (dto) => {
    const record = { recordType: dto.recordType };
    if (dto.mediaType) record.mediaType = dto.mediaType;
    if (dto.id) record.id = dto.id;
    if (dto.lang) record.lang = dto.lang;
    if (dto.encoding) record.encoding = dto.encoding;
    if (dto.records) record.data = { records: dto.records.map(fromDto) };
    else if (dto.text !== undefined && dto.text !== null) record.data = dto.text;
    else if (dto.data) record.data = fromBase64(dto.data);
    return record;
};

const ensureReader = () => {
    if (!("NDEFReader" in globalThis))
        throw new Error("NDEFReader unsupported in this browser");
    reader ??= new NDEFReader();
    return reader;
};

export async function permission() {
    if (!("NDEFReader" in globalThis))
        return "unsupported";
    if (!globalThis.isSecureContext)
        return "insecure";
    try {
        const status = await navigator.permissions.query({ name: "nfc" });
        return status.state;
    } catch {
        return "prompt";
    }
}

export async function start(dotnet) {
    const ndef = ensureReader();
    controller?.abort();
    controller = new AbortController();

    ndef.onreading = (e) => dotnet.invokeMethodAsync("OnReading", e.serialNumber || null, JSON.stringify(e.message.records.map(toDto)));
    ndef.onreadingerror = () => dotnet.invokeMethodAsync("OnReadingError");

    await ndef.scan({ signal: controller.signal });
}

export function stop() {
    controller?.abort();
    controller = null;
    if (reader) {
        reader.onreading = null;
        reader.onreadingerror = null;
    }
}

export async function write(recordsJson) {
    const records = JSON.parse(recordsJson).map(fromDto);
    await ensureReader().write({ records }, { signal: controller?.signal, overwrite: true });
}

export async function makeReadOnly() {
    await ensureReader().makeReadOnly({ signal: controller?.signal });
}
