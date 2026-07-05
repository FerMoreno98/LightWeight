import { Service } from '@angular/core';

@Service()
export class TokenStorageService {
// el access token vive en la ram, si el usuario recarga la pagina, se pierde
    private AccessToken : string | null = null;
    setAccessToken(accessToken : string){
        this.AccessToken = accessToken;
    }
    getAccessToken() : string | null{
        return this.AccessToken;
    }
    clear(){
        this.AccessToken = null;
    }
}
