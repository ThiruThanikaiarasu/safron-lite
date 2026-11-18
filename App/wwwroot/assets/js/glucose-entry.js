/* Glucose entry form */
$(function () {
    var LOW_LIMIT = 72;

    var $value = $('#value'),
        $confirm = $('#confirm_value'),
        $lowPanel = $('#low-panel'),
        $save = $('#btn-save');

    function checkLow() {
        var v = parseInt($value.val(), 10);
        if (!isNaN(v) && v <= LOW_LIMIT) {
            $lowPanel.removeClass('hidden');
        } else if ($('#api_low').val() !== '1') {
            $lowPanel.addClass('hidden');
            $('#ack_low').prop('checked', false);
        }
        updateSave();
    }

    function checkConfirm() {
        var a = $.trim($value.val()), b = $.trim($confirm.val());
        $('#confirm-help').toggleClass('hidden', b === '' || a === b);
    }

    function updateSave() {
        if (!$save.length) {
            return;
        }
        var ok = true;
        $('.ack:visible').each(function () {
            if (!this.checked) {
                ok = false;
            }
        });
        $save.prop('disabled', !ok);
    }

    $value.on('input change', checkLow);
    $confirm.on('input blur', checkConfirm);
    $(document).on('change', '.ack', updateSave);

    $('#entry-form').on('submit', function () {
        if ($save.length && $save.prop('disabled')) {
            return false;
        }
    });

    checkLow();
});
