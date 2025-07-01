import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:5286/api';
  private loggedIn = new BehaviorSubject<boolean>(this.hasToken());

  isLoggedIn$ = this.loggedIn.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  private hasToken(): boolean {
    return !!localStorage.getItem('token');
  }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // --- Auth Fonksiyonları ---

  // Register DTO: username, email, password
  register(userInfo: { username: string; email: string; password: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/Auth/register`, userInfo, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    }).pipe(
      tap((response: any) => {
        // Eğer backend register sonrası token gönderiyorsa (login gibi), token'ı sakla
        if (response && response.token) {
          localStorage.setItem('token', response.token);
          this.loggedIn.next(true);
        }
      })
    );
  }

  // Login DTO: username, password
  login(credentials: { username: string; password: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/Auth/login`, credentials, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    }).pipe(
      tap((response: any) => {
        if (response && response.token) {
          localStorage.setItem('token', response.token);
          this.loggedIn.next(true);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem('token');
    this.loggedIn.next(false);
    this.router.navigate(['/']);
  }
  addMapPoint(data: any) {
    return this.http.post('/api/MapPoint', data);
  }
  
  addMapArea(data: any) {
    return this.http.post('/api/MapArea', data);
  }

  // --- Harita İşlemleri ---

  createPoint(pointDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/MapPoint`, pointDto, {
      headers: this.getAuthHeaders()
    });
  }

  getAllPoints(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/MapPoint`, {
      headers: this.getAuthHeaders()
    });
  }

  updatePoint(id: number, pointDto: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/MapPoint/${id}`, pointDto, {
      headers: this.getAuthHeaders()
    });
  }

  deletePoint(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/MapPoint/${id}`, {
      headers: this.getAuthHeaders()
    });
  }

  createArea(areaDto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/MapArea`, areaDto, {
      headers: this.getAuthHeaders()
    });
  }

  getAllAreas(): Observable<{ type: string; features: any[] }> {
    return this.http.get<{ type: string; features: any[] }>(`${this.baseUrl}/MapArea`, {
      headers: this.getAuthHeaders()
    });
  }

  updateArea(id: number, areaDto: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/MapArea/${id}`, areaDto, {
      headers: this.getAuthHeaders()
    });
  }

  deleteArea(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/MapArea/${id}`, {
      headers: this.getAuthHeaders()
    });
  }
}