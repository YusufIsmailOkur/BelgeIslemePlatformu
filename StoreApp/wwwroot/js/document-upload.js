(function () {
    var dropZone = document.getElementById('dropZone');
    var fileInput = document.getElementById('fileInput');
    var fileList = document.getElementById('fileList');
    var uploadForm = document.getElementById('uploadForm');

    if (!dropZone || !fileInput || !fileList || !uploadForm) {
        return;
    }

    var antiForgeryToken = uploadForm.querySelector('input[name="__RequestVerificationToken"]').value;

    ['dragenter', 'dragover'].forEach(function (eventName) {
        dropZone.addEventListener(eventName, function (event) {
            event.preventDefault();
            event.stopPropagation();
            dropZone.classList.add('border-primary', 'bg-primary-subtle');
        });
    });

    ['dragleave', 'drop'].forEach(function (eventName) {
        dropZone.addEventListener(eventName, function (event) {
            event.preventDefault();
            event.stopPropagation();
            dropZone.classList.remove('border-primary', 'bg-primary-subtle');
        });
    });

    dropZone.addEventListener('drop', function (event) {
        var droppedFiles = event.dataTransfer && event.dataTransfer.files;
        if (droppedFiles && droppedFiles.length > 0) {
            uploadFiles(droppedFiles);
        }
    });

    fileInput.addEventListener('change', function () {
        if (fileInput.files.length > 0) {
            uploadFiles(fileInput.files);
            fileInput.value = '';
        }
    });

    function uploadFiles(filesToUpload) {
        var formData = new FormData();
        Array.prototype.forEach.call(filesToUpload, function (file) {
            formData.append('files', file);
        });

        var items = Array.prototype.map.call(filesToUpload, function (file) {
            return addListItem(file.name, 'Yükleniyor...', 'text-muted');
        });

        fetch('/Documents/Upload', {
            method: 'POST',
            headers: { 'RequestVerificationToken': antiForgeryToken },
            body: formData
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('İstek başarısız oldu.');
                }
                return response.json();
            })
            .then(function (results) {
                results.forEach(function (result, index) {
                    var item = items[index];
                    if (!item) {
                        return;
                    }
                    if (result.success) {
                        setListItemStatus(item, 'Yüklendi', 'text-success');
                    } else {
                        setListItemStatus(item, result.errorMessage || 'Hata oluştu', 'text-danger');
                    }
                });
            })
            .catch(function () {
                items.forEach(function (item) {
                    setListItemStatus(item, 'Yükleme başarısız oldu', 'text-danger');
                });
            });
    }

    function addListItem(fileName, statusText, statusClass) {
        var li = document.createElement('li');
        li.className = 'list-group-item d-flex justify-content-between align-items-center';

        var nameSpan = document.createElement('span');
        nameSpan.textContent = fileName;

        var statusSpan = document.createElement('span');
        statusSpan.className = statusClass;
        statusSpan.textContent = statusText;

        li.appendChild(nameSpan);
        li.appendChild(statusSpan);
        fileList.prepend(li);

        return statusSpan;
    }

    function setListItemStatus(statusSpan, text, statusClass) {
        statusSpan.className = statusClass;
        statusSpan.textContent = text;
    }
})();
