(function () {
    var form = document.getElementById('lineItemsForm');
    var table = document.getElementById('lineItemsTable');
    var addRowBtn = document.getElementById('addLineItemRow');

    if (!form || !table || !addRowBtn) {
        return;
    }

    var tbody = table.querySelector('tbody');
    var headerCellCount = table.querySelectorAll('thead th').length - 1; // son sütun "Sil" butonu için, alan değil.
    var fieldKeys = [];
    table.querySelectorAll('thead th').forEach(function (th, index) {
        if (index < headerCellCount) {
            fieldKeys.push(th.dataset.fieldKey);
        }
    });

    addRowBtn.addEventListener('click', function () {
        var row = document.createElement('tr');

        fieldKeys.forEach(function (fieldKey) {
            var cell = document.createElement('td');
            var input = document.createElement('input');
            input.type = 'text';
            input.className = 'form-control form-control-sm';
            input.name = 'lineItems[0][' + fieldKey + ']';
            cell.appendChild(input);
            row.appendChild(cell);
        });

        var actionCell = document.createElement('td');
        var removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-sm btn-outline-danger remove-line-item-row';
        removeBtn.textContent = 'Sil';
        actionCell.appendChild(removeBtn);
        row.appendChild(actionCell);

        tbody.appendChild(row);
    });

    tbody.addEventListener('click', function (event) {
        if (event.target.classList.contains('remove-line-item-row')) {
            event.target.closest('tr').remove();
        }
    });

    // Satır ekleme/silme sırasında input adlarındaki [index] boşluklu kalabilir
    // (ör. 0,2,3); ASP.NET Core'un liste binder'ı boşluk gördüğünde durur ve
    // sonraki satırları yoksayar. Bu yüzden gönderim öncesi sırayla yeniden numaralandırılır.
    form.addEventListener('submit', function () {
        var rows = tbody.querySelectorAll('tr');
        rows.forEach(function (row, rowIndex) {
            row.querySelectorAll('input').forEach(function (input) {
                input.name = input.name.replace(/lineItems\[\d+\]/, 'lineItems[' + rowIndex + ']');
            });
        });
    });
})();
