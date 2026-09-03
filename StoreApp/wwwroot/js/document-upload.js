(function () {
    var dropZone = document.getElementById('dropZone');
    var fileInput = document.getElementById('fileInput');
    var fileList = document.getElementById('fileList');
    var uploadForm = document.getElementById('uploadForm');
    var submitUploadBtn = document.getElementById('submitUploadBtn');

    if (!dropZone || !fileInput || !fileList || !uploadForm || !submitUploadBtn) {
        return;
    }

    var antiForgeryToken = uploadForm.querySelector('input[name="__RequestVerificationToken"]').value;

    // Seçilen/sürüklenen dosyalar hemen yüklenmez; kullanıcı "Yükle"ye basana kadar
    // burada bekler ve istediğini "Sil" ile çıkarabilir (Moodle tarzı kabul akışı).
    var stagedEntries = [];

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
            stageFiles(droppedFiles);
        }
    });

    fileInput.addEventListener('change', function () {
        if (fileInput.files.length > 0) {
            stageFiles(fileInput.files);
            fileInput.value = '';
        }
    });

    submitUploadBtn.addEventListener('click', function () {
        if (stagedEntries.length === 0) {
            return;
        }

        var entriesToUpload = stagedEntries;
        stagedEntries = [];
        refreshSubmitButtonState();

        entriesToUpload.forEach(function (entry) {
            entry.removeBtn.remove();
            entry.statusSpan = document.createElement('span');
            entry.statusSpan.className = 'text-muted';
            entry.statusSpan.textContent = 'Yükleniyor...';
            entry.li.appendChild(entry.statusSpan);
        });

        uploadFiles(entriesToUpload);
    });

    function stageFiles(filesToStage) {
        Array.prototype.forEach.call(filesToStage, function (file) {
            var entry = { file: file };

            var li = document.createElement('li');
            li.className = 'list-group-item d-flex justify-content-between align-items-center';

            var nameSpan = document.createElement('span');
            nameSpan.textContent = file.name;

            var removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'btn btn-sm btn-outline-danger';
            removeBtn.textContent = 'Sil';
            removeBtn.addEventListener('click', function () {
                var index = stagedEntries.indexOf(entry);
                if (index > -1) {
                    stagedEntries.splice(index, 1);
                }
                li.remove();
                refreshSubmitButtonState();
            });

            li.appendChild(nameSpan);
            li.appendChild(removeBtn);
            fileList.appendChild(li);

            entry.li = li;
            entry.removeBtn = removeBtn;
            stagedEntries.push(entry);
        });

        refreshSubmitButtonState();
    }

    function refreshSubmitButtonState() {
        submitUploadBtn.disabled = stagedEntries.length === 0;
    }

    function uploadFiles(entries) {
        var formData = new FormData();
        entries.forEach(function (entry) {
            formData.append('files', entry.file);
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
                    var entry = entries[index];
                    if (!entry) {
                        return;
                    }
                    if (result.success) {
                        var statusClass = result.statusBadgeClass ? 'badge ' + result.statusBadgeClass : 'text-success';
                        setListItemStatus(entry.statusSpan, result.statusDisplay || 'Yüklendi', statusClass);
                        if (result.documentId) {
                            addPreviewLink(entry.li, result.documentId);
                        }
                    } else {
                        setListItemStatus(entry.statusSpan, result.errorMessage || 'Hata oluştu', 'text-danger');
                    }
                });
            })
            .catch(function () {
                entries.forEach(function (entry) {
                    setListItemStatus(entry.statusSpan, 'Yükleme başarısız oldu', 'text-danger');
                });
            });
    }

    function setListItemStatus(statusSpan, text, statusClass) {
        statusSpan.className = statusClass;
        statusSpan.textContent = text;
    }

    function addPreviewLink(li, documentId) {
        var link = document.createElement('a');
        link.href = '/Documents/Preview/' + documentId;
        link.className = 'btn btn-sm btn-outline-secondary ms-2';
        link.textContent = 'İncele';
        li.appendChild(link);
    }
})();
