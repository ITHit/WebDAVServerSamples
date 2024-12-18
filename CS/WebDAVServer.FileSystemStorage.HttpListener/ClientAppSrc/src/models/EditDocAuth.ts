export class EditDocAuth {
    Authentication: string | null;
    CookieNames: string | null;
    SearchIn: string | null;
    LoginUrl: string | null;
    constructor(
        authentication: string | null,
        cookieNames: string | null,
        searchIn: string | null,
        loginUrl: string | null
    ) {
        this.Authentication = authentication;
        this.CookieNames = cookieNames;
        this.SearchIn = searchIn;
        this.LoginUrl = loginUrl;
    }
}