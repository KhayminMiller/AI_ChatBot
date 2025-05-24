// wwwroot/js/facecapture.js

console.log("✅ facecapture.js loaded");

window.startCamera = async function () {
    const video = document.getElementById("video");
    if (!video) {
        alert("❌ Video element not found.");
        return;
    }

    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        video.srcObject = stream;
    } catch (err) {
        console.error("Camera error:", err);
        alert("❌ Could not access the camera.");
    }
};

window.captureAndUploadPhoto = async function () {
    const video = document.getElementById("video");
    const canvas = document.getElementById("canvas");

    if (!video || !canvas) {
        alert("❌ Elements not found.");
        return;
    }

    const context = canvas.getContext("2d");
    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    const blob = await new Promise(resolve =>
        canvas.toBlob(resolve, "image/jpeg")
    );

    const formData = new FormData();
    formData.append("file", blob, "captured.jpg");

    try {
        const response = await fetch("/api/FaceDetection/upload", {
            method: "POST",
            body: formData
        });

        alert(response.ok ? "✅ Photo uploaded and faces detected." : "❌ Upload failed.");
    } catch (error) {
        console.error("Upload error:", error);
        alert("❌ Upload failed.");
    }
};
