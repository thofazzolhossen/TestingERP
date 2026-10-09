(function () {
    'use strict';
    if (window.__crmMessengerLoaded) return;
    window.__crmMessengerLoaded = true;

    $(function () {
        if (!window.abp || !abp.currentUser || !abp.currentUser.isAuthenticated) return;

        var BASE = '/Messenger';
        var state = { open: false, view: 'list', convId: null, convName: '', lastSig: '', timer: null };

        // ---------- DOM ----------
        var $fab = $('<button id="crmMsgFab" title="Messenger"><i class="fa-brands fa-facebook-messenger"></i><span id="crmMsgBadge"></span></button>');
        var $panel = $(
            '<div id="crmMsgPanel">' +
            '  <div class="crm-msg-head">' +
            '    <button id="crmMsgBack" style="display:none"><i class="fa fa-arrow-left"></i></button>' +
            '    <span class="title" id="crmMsgTitle">Messenger Inbox</span>' +
            '    <button id="crmMsgClose"><i class="fa fa-times"></i></button>' +
            '  </div>' +
            '  <div class="crm-msg-body" id="crmMsgBody"></div>' +
            '  <div class="crm-msg-foot" id="crmMsgFoot">' +
            '    <textarea id="crmMsgInput" placeholder="Type a reply..."></textarea>' +
            '    <button id="crmMsgSend"><i class="fa fa-paper-plane"></i></button>' +
            '  </div>' +
            '</div>');
        $('body').append($fab, $panel);

        // ---------- helpers ----------
        function esc(s) { return $('<div>').text(s == null ? '' : s).html(); }
        function toDate(s) { return new Date(/Z|[+-]\d\d:?\d\d$/.test(s) ? s : s + 'Z'); }
        function fmtTime(s) {
            var d = toDate(s), now = new Date();
            return d.toDateString() === now.toDateString()
                ? d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
                : d.toLocaleDateString([], { month: 'short', day: 'numeric' });
        }
        function call(type, url, data) {
            return abp.ajax({
                url: BASE + url, type: type, abpHandleError: false,
                data: data ? JSON.stringify(data) : undefined
            });
        }
        function setBadge(n) {
            var $b = $('#crmMsgBadge');
            n > 0 ? $b.text(n > 99 ? '99+' : n).show() : $b.hide();
        }

        // ---------- views ----------
        function loadUnread() {
            call('GET', '?handler=Unread').then(function (r) { setBadge(r.count); });
        }

        function showList() {
            state.view = 'list'; state.convId = null; state.lastSig = '';
            $('#crmMsgBack').hide();
            $('#crmMsgTitle').text('Messenger Inbox');
            $('#crmMsgFoot').removeClass('show');
            refreshList();
        }

        function refreshList() {
            call('GET', '?handler=Conversations').then(function (list) {
                if (state.view !== 'list') return;
                var total = 0, html = '';
                list.forEach(function (c) {
                    total += c.unreadCount;
                    var avatar = c.profilePictureUrl
                        ? '<img class="crm-msg-avatar" src="' + esc(c.profilePictureUrl) + '">'
                        : '<div class="crm-msg-avatar">' + esc((c.customerName || '?').charAt(0).toUpperCase()) + '</div>';
                    html += '<div class="crm-msg-item" data-id="' + c.id + '" data-name="' + esc(c.customerName) + '">' +
                        avatar +
                        '<div class="crm-msg-meta"><div class="name"><span>' + esc(c.customerName) + '</span><small>' +
                        fmtTime(c.lastMessageTime) + '</small></div>' +
                        '<div class="last ' + (c.unreadCount ? 'unread' : '') + '">' + esc(c.lastMessageText) + '</div></div>' +
                        (c.unreadCount ? '<span class="crm-msg-unread">' + c.unreadCount + '</span>' : '') +
                        '</div>';
                });
                $('#crmMsgBody').html(html || '<div class="crm-msg-empty"><i class="fa-brands fa-facebook-messenger fa-2x"></i><p class="mt-2">No conversations yet.</p></div>');
                setBadge(total);
            });
        }

        function openChat(id, name) {
            state.view = 'chat'; state.convId = id; state.convName = name; state.lastSig = '';
            $('#crmMsgBack').show();
            $('#crmMsgTitle').text(name);
            $('#crmMsgFoot').addClass('show');
            $('#crmMsgBody').html('<div class="crm-msg-empty">Loading...</div>');
            refreshChat(true);
            call('POST', '?handler=Read&id=' + id).then(loadUnread);
            $('#crmMsgInput').focus();
        }

        function refreshChat(forceScroll) {
            if (!state.convId) return;
            var id = state.convId;
            call('GET', '?handler=Messages&id=' + id).then(function (msgs) {
                if (state.view !== 'chat' || state.convId !== id) return;
                var sig = msgs.length + ':' + (msgs.length ? msgs[msgs.length - 1].id : '');
                if (sig === state.lastSig && !forceScroll) return;
                state.lastSig = sig;

                var html = '<div class="crm-msg-thread">';
                msgs.forEach(function (m) {
                    html += '<div class="crm-bubble ' + (m.isFromCustomer ? 'in' : 'out') + '">' +
                        esc(m.text) + '<small>' + fmtTime(m.sentTime) + '</small></div>';
                });
                html += '</div>';
                var body = document.getElementById('crmMsgBody');
                var nearBottom = body.scrollHeight - body.scrollTop - body.clientHeight < 80;
                $('#crmMsgBody').html(msgs.length ? html : '<div class="crm-msg-empty">No messages yet.</div>');
                if (forceScroll || nearBottom) body.scrollTop = body.scrollHeight;
                // mark read if new customer message arrived while chat is open
                call('POST', '?handler=Read&id=' + id);
            });
        }

        function sendMessage() {
            var $in = $('#crmMsgInput'), text = $.trim($in.val());
            if (!text || !state.convId) return;
            var $btn = $('#crmMsgSend').prop('disabled', true);
            abp.ajax({
                url: BASE + '?handler=Send&id=' + state.convId,
                type: 'POST',
                data: JSON.stringify({ text: text })
            }).then(function () {
                $in.val('');
                refreshChat(true);
            }).always(function () { $btn.prop('disabled', false); });
        }

        // ---------- polling ----------
        function startTimer() {
            stopTimer();
            state.timer = setInterval(function () {
                if (state.view === 'chat') refreshChat(false); else refreshList();
            }, 4000);
        }
        function stopTimer() { if (state.timer) clearInterval(state.timer); state.timer = null; }

        // ---------- events ----------
        $fab.on('click', function () {
            state.open = !state.open;
            $panel.toggleClass('open', state.open);
            if (state.open) { showList(); startTimer(); } else { stopTimer(); }
        });
        $('#crmMsgClose').on('click', function () { state.open = false; $panel.removeClass('open'); stopTimer(); loadUnread(); });
        $('#crmMsgBack').on('click', function () { showList(); loadUnread(); });
        $('#crmMsgBody').on('click', '.crm-msg-item', function () {
            openChat($(this).data('id'), $(this).attr('data-name'));
        });
        $('#crmMsgSend').on('click', sendMessage);
        $('#crmMsgInput').on('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); }
        });

        // background unread badge
        loadUnread();
        setInterval(function () { if (!state.open) loadUnread(); }, 20000);
    });
})();