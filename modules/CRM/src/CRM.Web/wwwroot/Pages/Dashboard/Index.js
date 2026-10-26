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

    // Create once, then only swap data (no re-allocation on refresh)
    function upsert(id, type, labels, data, opts, colors) {
        const bg = colors || labels.map((_, i) => PALETTE[i % PALETTE.length]);
        if (charts[id]) {
            const c = charts[id];
            c.data.labels = labels;
            c.data.datasets[0].data = data;
            if (!colors && type !== 'line') c.data.datasets[0].backgroundColor = bg;
            c.update();
            return;
        }
        charts[id] = new Chart(document.getElementById(id), {
            type,
            data: { labels, datasets: [{ data, backgroundColor: bg, borderWidth: 0, borderRadius: type === 'bar' ? 8 : 0 }] },
            options: opts
        });
    }

    const noGrid = { grid: { display: false }, border: { display: false } };
    const softGrid = { grid: { color: '#f1f5f9' }, border: { display: false }, beginAtZero: true, ticks: { precision: 0 } };

    function trend(items) {
        const ctx = document.getElementById('chTrend').getContext('2d');
        const g = ctx.createLinearGradient(0, 0, 0, 260);
        g.addColorStop(0, 'rgba(99,102,241,.35)'); g.addColorStop(1, 'rgba(99,102,241,0)');
        const labels = items.map(x => new Date(x.date).toLocaleDateString([], { month: 'short', day: 'numeric' }));
        const data = items.map(x => x.count);
        if (charts.chTrend) {
            charts.chTrend.data.labels = labels; charts.chTrend.data.datasets[0].data = data; charts.chTrend.update(); return;
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
        // KPIs
        $('.kpi .value').removeClass('skel');
        const delta = d.newLastMonth === 0 ? (d.newThisMonth > 0 ? 100 : 0)
            : Math.round((d.newThisMonth - d.newLastMonth) / d.newLastMonth * 100);
        $('#kTotal').text(num(d.totalLeads));
        $('#kTotalSub').text(`${num(d.unassigned)} unassigned`);
        $('#kValue').text(money(d.totalBudget));
        $('#kValueSub').text(`avg ${money(d.avgBudget)} / lead`);
        $('#kNew').text(num(d.newThisMonth));
        $('#kNewSub').html(`<span class="pill up">${delta >= 0 ? '▲' : '▼'} ${Math.abs(delta)}%</span> vs last month`);
        $('#kToday').text(num(d.dueToday));
        $('#kTodaySub').text(d.dueToday ? 'follow-ups to make' : 'all clear');
        $('#kLate').text(num(d.overdueFollowUps));
        $('#kLateSub').text(d.overdueFollowUps ? 'need attention now' : 'nothing overdue 🎉');

        // Charts
        trend(d.trend);

        upsert('chStage', 'doughnut', d.byStage.map(x => x.name), d.byStage.map(x => x.count), {
            cutout: '68%',
            plugins: {
                legend: { position: 'bottom' },
                tooltip: { callbacks: { label: c => ` ${c.label}: ${c.parsed} leads · ${money(d.byStage[c.dataIndex].value)}` } }
            }
        });

        upsert('chSource', 'bar', d.bySource.map(x => x.name), d.bySource.map(x => x.count), {
            plugins: { legend: { display: false } }, scales: { x: noGrid, y: softGrid }
        });

        upsert('chService', 'bar', d.byService.slice(0, 7).map(x => x.name), d.byService.slice(0, 7).map(x => x.count), {
            indexAxis: 'y', plugins: { legend: { display: false } }, scales: { x: softGrid, y: noGrid }
        });

        upsert('chType', 'pie', d.byCustomerType.map(x => x.name), d.byCustomerType.map(x => x.count), {
            plugins: { legend: { position: 'bottom' } }
        });

        // Leaderboard
        const max = Math.max(1, ...d.topAssignees.map(x => x.count));
        $('#rank').html(d.topAssignees.length ? d.topAssignees.map((u, i) => `
            <div class="rank">
                <span class="fw-bold text-muted" style="width:18px">${i + 1}</span>
                <div style="width:110px" class="text-truncate fw-semibold">${esc(u.name)}</div>
                <div class="bar"><i style="width:${u.count / max * 100}%"></i></div>
                <span class="fw-bold">${u.count}</span>
                <small class="text-muted" style="width:52px;text-align:right">${money(u.value)}</small>
            </div>`).join('') : '<div class="empty">No assigned leads yet</div>');

        // Follow-ups
        const today = new Date(); today.setHours(0, 0, 0, 0);
        $('#followups').html(d.upcoming.length ? d.upcoming.map(f => {
            const dt = new Date(f.date); const day = new Date(dt); day.setHours(0, 0, 0, 0);
            const diff = Math.round((day - today) / 864e5);
            const cls = diff < 0 ? 'late' : diff === 0 ? 'today' : 'soon';
            const txt = diff < 0 ? `${-diff}d late` : diff === 0 ? 'Today' : `${diff}d`;
            return `<div class="fu">
                <div class="dot ${cls}">${txt}</div>
                <div class="flex-grow-1 text-truncate">
                    <div class="fw-semibold text-truncate">${esc(f.name)}</div>
                    <small class="text-muted">${esc(f.companyName || '—')} · ${dt.toLocaleDateString([], { month: 'short', day: 'numeric' })}</small>
                </div>
                <span class="fw-bold text-success">${money(f.budget)}</span>
            </div>`;
        }).join('') : '<div class="empty">No follow-ups scheduled</div>');

        $('#updated').text('updated ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));
    }

    let busy = false;
    function load() {
        if (busy || document.hidden) return;          // skip when tab hidden / request in flight
        busy = true;
        $.get(window.dashUrl).done(render)
            .fail(() => abp.notify.error('Failed to load dashboard.'))
            .always(() => busy = false);
    }

    $('#btnRefresh').on('click', load);
    load();
    setInterval(load, 60000);
});