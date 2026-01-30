// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
    // 1. Staggered Animation for Table Rows
    // Adds a slight delay to each row to create a cascading effect
    $('table tbody tr').each(function (index) {
        $(this).css({
            'opacity': '0',
            'transform': 'translateY(20px)',
            'transition': 'all 0.4s ease-out'
        });
        
        var $row = $(this);
        setTimeout(function () {
            $row.css({
                'opacity': '1',
                'transform': 'translateY(0)'
            });
        }, 100 * index); // 100ms delay per row
    });

    // 2. Button Loading State
    // When a form with a submit button is submitted, change button text/icon
    $('form').on('submit', function () {
        if ($(this).valid()) { // Check if form is valid (using jQuery Validation)
            var $btn = $(this).find('button[type="submit"]');
            var originalText = $btn.html();
            
            // Disable button to prevent double submit
            $btn.prop('disabled', true);
            
            // Add spinner
            $btn.html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Processing...');
            
            // If it takes too long (e.g. server timeout), re-enable (optional, safety net)
            setTimeout(function() {
                $btn.prop('disabled', false);
                $btn.html(originalText);
            }, 10000);
        }
    });

    // 3. Hover Effects for Cards (JS fallback if CSS hover isn't enough, but CSS is preferred)
    // We already have CSS hover, so we can add a 'tilt' effect here if we want to be fancy.
    // For now, let's keep it simple.

    // 4. Initialize Tooltips (if using Bootstrap Tooltips elsewhere)
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    })
});
