$(document).ready(function () {
    //Hide Menus when document ready.
    $("#GLOTCEQOptions").hide();

    //Show specific menu when clicked, hide all others.
    $("#GLOTCEQMenu").click(function () {
        $("#GLOTCEQOptions").toggle();
    });
});