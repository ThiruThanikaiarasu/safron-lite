/* Patient detail - order actions */
$(function () {
    var tenant = $('body').data('tenant');

    function orderAction(btn, action, question) {
        if (!window.confirm(question)) {
            return;
        }
        var $btn = $(btn);
        $btn.prop('disabled', true);
        $.ajax({
            url: '/api/orders/' + $btn.data('order-id') + '/' + action,
            type: 'POST',
            dataType: 'json',
            headers: { 'X-Saffron-Tenant': tenant }
        }).done(function () {
            window.location.reload();
        }).fail(function (xhr) {
            var msg = (xhr.responseJSON && xhr.responseJSON.message) || ('Request failed (' + xhr.status + ')');
            window.alert(msg);
            $btn.prop('disabled', false);
        });
    }

    $('#btn-discontinue').on('click', function () {
        orderAction(this, 'discontinue', 'Discontinue this infusion order?');
    });

    $('#btn-resume').on('click', function () {
        orderAction(this, 'resume', 'Resume this infusion order?');
    });
});
