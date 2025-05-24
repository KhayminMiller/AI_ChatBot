window.downloadFile = async function (fileName, streamRef) {
    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName ?? "download";
    anchor.click();

    URL.revokeObjectURL(url);
}
