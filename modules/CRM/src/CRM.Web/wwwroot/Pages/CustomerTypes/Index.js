$(document).ready(function () {

    // =========================================================
    // GRID INITIALIZATION
    // =========================================================
    window.customerTypeGrid = createCommonGrid({
        gridId: "CustomerTypesGrid",
        title: "Customer Types",
        exportFileName: "customer-types.csv",
        height: "550px",

        buttons: [
            {
                id: "BtnCreateNew",
                html: `<i class="fa fa-plus"></i> New Customer Type`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    $('#CustomerTypeForm')[0].reset();
                    $('#CustomerTypeId').val('');
                    $('#TypeIsActive').prop('checked', true);
                    $('#ModalTitle').text('New Customer Type');
                    $('#CustomerTypeModal').modal('show');
                }
            }
        ],

        columnDefs: [
            {
                field: "name",
                headerName: "Name",
                flex: 1
            },
            {
                field: "description",
                headerName: "Description",
                flex: 2,
                valueFormatter: params => params.value || '-'
            },
            {
                field: "isActive",
                headerName: "Status",
                width: 130,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    return params.value
                        ? `<span class="badge bg-success-subtle text-success px-2 py-1"><i class="fa-solid fa-circle-check me-1"></i>Active</span>`
                        : `<span class="badge bg-secondary-subtle text-secondary px-2 py-1"><i class="fa-solid fa-circle-xmark me-1"></i>Inactive</span>`;
                }
            },
            {
                headerName: "Actions",
                width: 120,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';

                    const encodedRow = encodeURIComponent(JSON.stringify(params.data));

                    return `
                        <div class="d-flex align-items-center h-100 gap-2">
                            <button type="button" class="btn btn-sm btn-outline-primary border-0 edit-button" 
                                    data-row="${encodedRow}" title="Edit">
                                <i class="fa-solid fa-pen-to-square"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger border-0 delete-button" 
                                    data-id="${params.data.id}" data-name="${params.data.name}" title="Delete">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],

        // Client-side loading (same pattern as Lead Stages)
        rowData: function () {
            return $.ajax({
                url: window.customerTypeUrls.list,
                type: 'GET',
                data: { start: 0, length: 1000 }
            }).then(response => response.data || response.items || []);
        }
    });


    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    // ---------- OPEN EDIT MODAL ----------
    $(document).on('click', '#CustomerTypesGrid .edit-button', function () {
        const rawData = $(this).attr('data-row');
        if (!rawData) return;

        const data = JSON.parse(decodeURIComponent(rawData));

        $('#CustomerTypeId').val(data.id || data.Id || '');
        $('#TypeName').val(data.name || data.Name || '');
        $('#TypeDescription').val(data.description || data.Description || '');
        $('#TypeIsActive').prop('checked', Boolean(data.isActive ?? data.IsActive));

        $('#ModalTitle').text('Edit Customer Type');
        $('#CustomerTypeModal').modal('show');
    });


    // ---------- SAVE (CREATE + EDIT) ----------
    $('#SaveButton').on('click', function () {
        const $btn = $(this);
        const id = $('#CustomerTypeId').val();
        const isEdit = !!id;

        const name = $('#TypeName').val();
        if (!name || name.trim() === '') {
            abp.notify.error('Please enter a Customer Type name.');
            return;
        }

        $btn.prop('disabled', true);

        const token = $('#CustomerTypeForm input[name="__RequestVerificationToken"]').val()
            || $('input[name="__RequestVerificationToken"]').val();

        const formData = {
            Name: name.trim(),
            Description: $('#TypeDescription').val(),
            IsActive: $('#TypeIsActive').is(':checked'),
            __RequestVerificationToken: token
        };

        let url = isEdit ? window.customerTypeUrls.update : window.customerTypeUrls.create;

        if (isEdit) {
            formData.id = id;
            // Some ABP handlers need id in the query string
            if (url && !url.includes('id=')) {
                url += (url.includes('?') ? '&' : '?') + 'id=' + encodeURIComponent(id);
            }
        }

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    abp.notify.success(isEdit ? 'Customer Type updated successfully.' : 'Customer Type created successfully.');
                    $('#CustomerTypeModal').modal('hide');
                    $('#CustomerTypeForm')[0].reset();
                    window.customerTypeGrid.refresh();
                } else {
                    abp.notify.error(response.message || 'Operation failed.');
                }
            },
            error: function () {
                abp.notify.error(isEdit ? 'Update failed' : 'Create failed');
            },
            complete: function () {
                $btn.prop('disabled', false);
            }
        });
    });


    // ---------- DELETE ----------
    $(document).on('click', '#CustomerTypesGrid .delete-button', function () {
        const id = $(this).data('id');
        const name = $(this).data('name');

        abp.message.confirm(`Are you sure you want to delete "${name}"?`, 'Confirm Deletion')
            .then(function (confirmed) {
                if (!confirmed) return;

                const token = $('input[name="__RequestVerificationToken"]').first().val();

                $.ajax({
                    url: window.customerTypeUrls.delete,
                    type: 'POST',
                    data: {
                        id: id,
                        __RequestVerificationToken: token
                    },
                    success: function (response) {
                        if (response.success) {
                            abp.notify.success('Deleted successfully.');
                            window.customerTypeGrid.refresh();
                        } else {
                            abp.notify.error(response.message || 'Failed to delete');
                        }
                    },
                    error: function () {
                        abp.notify.error('Failed to delete Customer Type.');
                    }
                });
            });
    });

});