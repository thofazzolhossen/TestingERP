
$(function () {
    let stageHistoryLoaded = false;
    let stageHistoryItems = [];

    const $details = $('#SectionDetails');
    const $history = $('#SectionStageRecord');
    const $detailsButton = $('#BtnDetails');
    const $historyButton = $('#BtnStageHistory');

    function escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, function (char) {
            return {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#039;'
            }[char];
        });
    }

    function formatDateTime(value) {
        if (!value) return '—';

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return String(value);

        return date.toLocaleString(undefined, {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function showSection(section) {
        const showHistory = section === 'history';

        $details.toggleClass('d-none', showHistory);
        $history.toggleClass('d-none', !showHistory);

        $detailsButton.toggleClass('active', !showHistory)
            .attr('aria-pressed', String(!showHistory));

        $historyButton.toggleClass('active', showHistory)
            .attr('aria-pressed', String(showHistory));

        if (showHistory && !stageHistoryLoaded) {
            loadStageHistory();
        }
    }

    $detailsButton.on('click', function () {
        showSection('details');
    });

    $historyButton.on('click', function () {
        showSection('history');
    });

    $('#RetryStageHistory').on('click', loadStageHistory);

    $('#StageHistorySearch').on('input', function () {
        renderHistory($(this).val());
    });

    function getValue(item, camelCase, pascalCase, fallback) {
        const value = item?.[camelCase] ?? item?.[pascalCase];
        return value === null || value === undefined || value === ''
            ? fallback
            : value;
    }

    function loadStageHistory() {
        const config = window.leadDetailConfig;

        if (!config?.stageHistoryUrl || !config?.leadId) {
            showError('Lead history URL or lead ID is missing.');
            return;
        }

        $('#StageHistoryLoader').removeClass('d-none');
        $('#StageHistoryContent, #StageHistoryEmpty, #StageHistoryError')
            .addClass('d-none');

        $.ajax({
            url: config.stageHistoryUrl,
            type: 'GET',
            dataType: 'json',
            cache: false
        })
            .done(function (response) {
                // Supports the current Razor Page JSON response:
                // { success: true, data: [...] }
                // Also tolerates a direct array or common wrapped responses.
                let items;

                if (Array.isArray(response)) {
                    items = response;
                } else if (Array.isArray(response?.data)) {
                    items = response.data;
                } else if (Array.isArray(response?.Data)) {
                    items = response.Data;
                } else if (Array.isArray(response?.result?.data)) {
                    items = response.result.data;
                } else if (Array.isArray(response?.items)) {
                    items = response.items;
                } else if (Array.isArray(response?.Items)) {
                    items = response.Items;
                } else {
                    throw new Error('The server response did not contain a history array.');
                }

                stageHistoryItems = items;
                stageHistoryLoaded = true;

                $('#StageRecordCount').text(items.length);
                $('#StageHistorySearch').val('');

                renderHistory('');
            })
            .fail(function (xhr, status, error) {
                console.error('Stage history request failed:', {
                    status: xhr.status,
                    error: error,
                    response: xhr.responseText
                });

                showError(
                    xhr.status === 401 || xhr.status === 403
                        ? 'You do not have permission to view this history.'
                        : 'The history could not be loaded. Please try again.'
                );
            })
            .always(function () {
                $('#StageHistoryLoader').addClass('d-none');
            });
    }

    function showError(message) {
        $('#StageHistoryError')
            .removeClass('d-none')
            .contents()
            .filter(function () {
                return this.nodeType === 3;
            })
            .first()
            .replaceWith(' ' + message + ' ');
    }

    function renderHistory(searchText) {
        const query = String(searchText || '').trim().toLowerCase();

        const filteredItems = stageHistoryItems.filter(function (item) {
            const oldStage = getValue(
                item, 'oldStageName', 'OldStageName', 'Initial Creation'
            );
            const newStage = getValue(
                item, 'newStageName', 'NewStageName', 'Unknown Stage'
            );
            const user = getValue(
                item, 'changedByUserName', 'ChangedByUserName', 'System'
            );
            const remarks = getValue(item, 'remarks', 'Remarks', '');

            return [oldStage, newStage, user, remarks]
                .some(value => String(value).toLowerCase().includes(query));
        });

        $('#StageTimelineList').empty();
        $('#StageHistoryTableBody').empty();

        if (stageHistoryItems.length === 0) {
            $('#StageHistoryContent, #StageHistoryError')
                .addClass('d-none');
            $('#StageHistoryEmpty').removeClass('d-none');
            return;
        }

        $('#StageHistoryEmpty, #StageHistoryError')
            .addClass('d-none');
        $('#StageHistoryContent').removeClass('d-none');

        $('#StageHistoryNoSearchResults')
            .toggleClass('d-none', filteredItems.length !== 0);

        filteredItems.forEach(function (item, index) {
            const oldStage = escapeHtml(getValue(
                item, 'oldStageName', 'OldStageName', 'Initial Creation'
            ));
            const newStage = escapeHtml(getValue(
                item, 'newStageName', 'NewStageName', 'Unknown Stage'
            ));
            const user = escapeHtml(getValue(
                item, 'changedByUserName', 'ChangedByUserName', 'System'
            ));
            const remarks = escapeHtml(getValue(
                item, 'remarks', 'Remarks', 'No remarks'
            ));
            const dateValue = getValue(
                item, 'creationTime', 'CreationTime', null
            );
            const date = escapeHtml(formatDateTime(dateValue));

            const timelineItem = `
                <div class="stage-timeline-item">
                    <div class="stage-timeline-dot"></div>
                    <div class="stage-history-card">
                        <div class="d-flex justify-content-between align-items-center gap-2 flex-wrap">
                            <span class="text-muted small">
                                <i class="fa fa-clock me-1"></i>${date}
                            </span>
                            <span class="lead-pill lead-pill-success">
                                <i class="fa fa-check-circle"></i>Recorded
                            </span>
                        </div>

                        <div class="stage-transition">
                            <span class="stage-name">${oldStage}</span>
                            <i class="fa fa-arrow-right text-primary small"></i>
                            <span class="stage-name stage-name-new">${newStage}</span>
                        </div>

                        <div class="small text-muted">
                            <i class="fa fa-user me-1"></i>
                            Changed by <strong class="text-dark">${user}</strong>
                        </div>

                        <div class="small text-secondary mt-2">
                            ${remarks}
                        </div>
                    </div>
                </div>
            `;

            const tableRow = `
                <tr>
                    <td class="text-muted">${index + 1}</td>
                    <td>
                        <div class="d-flex align-items-center gap-2">
                            <span class="stage-name">${oldStage}</span>
                            <i class="fa fa-arrow-right text-muted"></i>
                            <span class="stage-name stage-name-new">${newStage}</span>
                        </div>
                    </td>
                    <td class="fw-semibold">${user}</td>
                    <td class="text-muted">${date}</td>
                    <td class="text-secondary">${remarks}</td>
                </tr>
            `;

            $('#StageTimelineList').append(timelineItem);
            $('#StageHistoryTableBody').append(tableRow);
        });
    }

    // Keep the overview visible on initial page load.
    showSection('details');
});
