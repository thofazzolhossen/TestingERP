$(function () {
    const PALETTE = ['#6366f1', '#06b6d4', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6', '#3b82f6', '#f97316'];
    const num = n => new Intl.NumberFormat().format(n || 0);
    const money = n => new Intl.NumberFormat('en', { notation: 'compact', maximumFractionDigits: 1 }).format(n || 0);
    const esc = s => $('<div>').text(s ?? '').html();
    const charts = {};

    Chart.defaults.font.family = 'Inter, system-ui, sans-serif';
    Chart.defaults.color = '#64748b';
    Chart.defaults.plugins.legend.labels.usePointStyle = true;
    Chart.defaults.plugins.legend.labels.boxWidth = 8;
    Chart.defaults.maintainAspectRatio = false;

    function upsert(id, type, labels, data, opts, colors) {
        const el = document.getElementById(id);
        if (!el || !labels || !data) return;
        const bg = colors || labels.map((_, i) => PALETTE[i % PALETTE.length]);
        if (charts[id]) {
            const c = charts[id];
            c.data.labels = labels;
            c.data.datasets[0].data = data;
            if (!colors && type !== 'line') c.data.datasets[0].backgroundColor = bg;
            c.update();
            return;
        }
        charts[id] = new Chart(el, {
            type,
            data: { labels, datasets: [{ data, backgroundColor: bg, borderWidth: 0, borderRadius: type === 'bar' ? 8 : 0 }] },
            options: opts
        });
    }

    const noGrid = { grid: { display: false }, border: { display: false } };
    const softGrid = { grid: { color: '#f1f5f9' }, border: { display: false }, beginAtZero: true, ticks: { precision: 0 } };

    function trend(items) {
        const ctxEl = document.getElementById('chTrend');
        if (!ctxEl || !items) return;
        const ctx = ctxEl.getContext('2d');
        const g = ctx.createLinearGradient(0, 0, 0, 260);
        g.addColorStop(0, 'rgba(99,102,241,.35)');
        g.addColorStop(1, 'rgba(99,102,241,0)');
        const labels = items.map(x => new Date(x.date).toLocaleDateString([], { month: 'short', day: 'numeric' }));
        const data = items.map(x => x.count);
        if (charts.chTrend) {
            charts.chTrend.data.labels = labels;
            charts.chTrend.data.datasets[0].data = data;
            charts.chTrend.update();
            return;
        }
        charts.chTrend = new Chart(ctx, {
            type: 'line',
            data: { labels, datasets: [{ data, borderColor: '#6366f1', backgroundColor: g, fill: true, tension: .38, borderWidth: 3, pointRadius: 0, pointHoverRadius: 6 }] },
            options: {
                interaction: { intersect: false, mode: 'index' },
                plugins: { legend: { display: false } },
                scales: { x: { ...noGrid, ticks: { maxTicksLimit: 8 } }, y: softGrid }
            }
        });
    }

    function render(d) {
        if (!d) return;

        // KPIs
        $('.kpi-card .kpi-value, .kpi .value').removeClass('skel');
        const delta = (d.newLastMonth || 0) === 0
            ? ((d.newThisMonth || 0) > 0 ? 100 : 0)
            : Math.round((((d.newThisMonth || 0) - d.newLastMonth) / d.newLastMonth) * 100);

        $('#kTotal').text(num(d.totalLeads));
        $('#kTotalSub').text(`${num(d.unassigned)} unassigned`);
        $('#kValue').text(money(d.totalBudget));
        $('#kAvgSub').text(`avg ${money(d.avgBudget)} / lead`);
        $('#kNew').text(num(d.newThisMonth));
        $('#kNewSub').html(`<span class="pill up">${delta >= 0 ? '▲' : '▼'} ${Math.abs(delta)}%</span> vs last month`);
        $('#kToday').text(num(d.dueToday));
        $('#kTodaySub').text(d.dueToday ? 'follow-ups to make' : 'all clear');
        $('#kLate').text(num(d.overdueFollowUps));
        $('#kLateSub').text(d.overdueFollowUps ? 'need attention now' : 'nothing overdue 🎉');

        // Charts
        trend(d.trend || []);

        upsert('chSource', 'bar', (d.bySource || []).map(x => x.name), (d.bySource || []).map(x => x.count), {
            plugins: { legend: { display: false } }, scales: { x: noGrid, y: softGrid }
        });

        upsert('chService', 'bar', (d.byService || []).slice(0, 7).map(x => x.name), (d.byService || []).slice(0, 7).map(x => x.count), {
            indexAxis: 'y', plugins: { legend: { display: false } }, scales: { x: softGrid, y: noGrid }
        });

        upsert('chType', 'pie', (d.byCustomerType || []).map(x => x.name), (d.byCustomerType || []).map(x => x.count), {
            plugins: { legend: { position: 'bottom' } }
        });

        // 1. Sales Pipeline
        const pipelineContainer = $('#pipelineContainer');
        if (pipelineContainer.length) {
            const stages = d.byStage || [];
            const totalStageCount = stages.reduce((sum, x) => sum + (x.count || 0), 0) || 1;
            const stageColors = ['#4263eb', '#e99532', '#7950c5', '#20a464', '#dc5a5a', '#06b6d4'];

            pipelineContainer.html(stages.length ? stages.map((stage, idx) => {
                const percent = Math.round(((stage.count || 0) / totalStageCount) * 100);
                const color = stageColors[idx % stageColors.length];
                return `
                    <div class="pipeline-row mb-3">
                        <div class="pipeline-label d-flex justify-content-between mb-1" style="font-size: 12px;">
                            <span class="fw-semibold">${esc(stage.name)}</span>
                            <span class="text-secondary">${stage.count || 0} (${percent}%) &middot; ${money(stage.value)}</span>
                        </div>
                        <div class="progress" style="height: 8px; background: #edf0f5; border-radius: 20px;">
                            <div class="progress-bar" style="width: ${percent}%; background: ${color}; border-radius: 20px;"></div>
                        </div>
                    </div>
                `;
            }).join('') : '<div class="text-center text-secondary py-3">No stage data available</div>');
        }

        // 2. Leaderboard / Top Performers
        const rankContainer = $('#rankContainer');
        if (rankContainer.length) {
            const topAssignees = d.topAssignees || [];
            const max = Math.max(1, ...topAssignees.map(x => x.count || 0));

            rankContainer.html(topAssignees.length ? topAssignees.map((u, i) => `
                <div class="rank-item d-flex align-items-center gap-2 mb-2">
                    <span class="fw-bold text-muted" style="width:18px">${i + 1}</span>
                    <div style="width:110px" class="text-truncate fw-semibold" title="${esc(u.name)}">${esc(u.name)}</div>
                    <div class="rank-bar flex-grow-1" style="height:8px; border-radius:6px; background:#e2e8f0; overflow:hidden;"><i style="display:block; height:100%; width:${Math.round(((u.count || 0) / max) * 100)}%; background:linear-gradient(90deg,#4263eb,#06b6d4); border-radius:6px;"></i></div>
                    <span class="fw-bold ms-2">${u.count || 0}</span>
                    <small class="text-muted" style="width:52px;text-align:right">${money(u.value)}</small>
                </div>`).join('') : '<div class="text-center text-secondary py-4">No assigned leads yet</div>');
        }

        // 3. Follow-up Overview
        const followupContainer = $('#followupContainer');
        if (followupContainer.length) {
            const upcoming = d.upcoming || [];
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            $('#overdueBadge').text(`${d.overdueFollowUps || 0} Overdue`);

            followupContainer.html(upcoming.length ? upcoming.map(f => {
                const dt = f.date ? new Date(f.date) : new Date();
                const day = new Date(dt);
                day.setHours(0, 0, 0, 0);
                const diff = Math.round((day - today) / 86400000);
                const badgeClass = diff < 0 ? 'badge-danger' : diff === 0 ? 'badge-warning' : 'badge-info';
                const badgeText = diff < 0 ? 'Overdue' : diff === 0 ? 'Today' : 'Upcoming';
                return `
                    <div class="followup d-flex justify-content-between align-items-center py-2 border-bottom">
                        <div>
                            <div class="followup-name fw-semibold">${esc(f.name)}</div>
                            <div class="followup-info text-muted small">${esc(f.companyName || '—')}</div>
                        </div>
                        <div class="text-end">
                            <span class="badge-soft ${badgeClass}">${badgeText}</span>
                            <div class="followup-info text-muted small">${dt.toLocaleDateString([], { month: 'short', day: 'numeric' })}</div>
                        </div>
                    </div>`;
            }).join('') : '<div class="text-center text-secondary py-4">No follow-ups scheduled</div>');
        }

        $('#updated').text('updated ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));
    }

    let busy = false;
    function load() {
        if (busy || document.hidden) return;
        busy = true;
        $.get(window.dashUrl).done(render)
            .fail(() => {
                if (typeof abp !== 'undefined' && abp.notify) {
                    abp.notify.error('Failed to load dashboard.');
                } else {
                    console.error('Failed to load dashboard data.');
                }
            })
            .always(() => busy = false);
    }

    $('#btnRefresh').on('click', load);
    load();
    setInterval(load, 60000);
});