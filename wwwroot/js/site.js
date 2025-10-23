// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
//

document.addEventListener("DOMContentLoaded", function () {
    const logoutButton = document.getElementById("LogoutButton");
    if (logoutButton) {
        logoutButton.addEventListener("click", async function () {
            localStorage.removeItem("token");

            await fetch("api/GoogleAuth/logout", {
                method: "POST"
            }).then(response => console.log(response));

            window.location.href = "/";
        });
    }


    const loginButton = document.getElementById("LoginButton");
    if (loginButton) {
        loginButton.addEventListener("click", function () {
            window.location.href = "/api/GoogleAuth/signin";
        });
    }
});

