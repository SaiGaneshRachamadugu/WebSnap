mergeInto(LibraryManager.library, {
  StartCamera: function () {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      alert("Camera API not supported");
      return;
    }

    const constraints = { video: { facingMode: "user" } };
    navigator.mediaDevices.getUserMedia(constraints)
      .then(function (stream) {
        window.localStream = stream;
        let video = document.getElementById('unityCameraVideo');
        if (!video) {
          video = document.createElement('video');
          video.id = 'unityCameraVideo';
          video.style.position = 'fixed';
          video.style.top = '10px';
          video.style.left = '10px';
          video.style.width = '320px';
          video.style.height = '240px';
          video.autoplay = true;
          video.playsInline = true;
          document.body.appendChild(video);
        }
        video.srcObject = stream;
      })
      .catch(function (err) {
        alert("Camera permission denied or error: " + err);
      });
  },

  CaptureImage: function () {
    const video = document.getElementById('unityCameraVideo');
    if (!video) {
      alert("No video element found");
      return;
    }

    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth || 320;
    canvas.height = video.videoHeight || 240;

    const ctx = canvas.getContext('2d');
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
    const dataUrl = canvas.toDataURL("image/png");

    if (window.localStream) {
      window.localStream.getTracks().forEach(track => track.stop());
      window.localStream = null;
    }
    video.remove();

    const base64Data = dataUrl.split(',')[1];

   
    if (typeof unityInstance !== 'undefined') {
      unityInstance.SendMessage("WebGLCamera", "OnCapturedImage", base64Data);
    } else {
      console.error("unityInstance not found");
    }
  },

  DownloadImage: function (base64Ptr, length) {
    var base64 = UTF8ToString(base64Ptr, length);
    var link = document.createElement('a');
    link.download = 'ghibli_style_image.png';
    link.href = 'data:image/png;base64,' + base64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
});
