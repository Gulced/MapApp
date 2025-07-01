import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api';
import { Router } from '@angular/router';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth.html',
  styleUrls: ['./auth.css']
})
export class AuthComponent implements OnInit {
  isLoginMode = true;
  username = '';  // kullanıcı adı (login ve register için)
  email = '';     // sadece register için
  password = '';
  errorMessage = '';

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {}

  toggleMode() {
    this.isLoginMode = !this.isLoginMode;
    this.errorMessage = '';
    this.username = '';
    this.email = '';
    this.password = '';
  }

  onSubmit() {
    this.errorMessage = '';

    if (this.isLoginMode) {
      // Login
      const credentials = {
        username: this.username.trim(),
        password: this.password
      };

      if (!credentials.username || !credentials.password) {
        this.errorMessage = 'Lütfen kullanıcı adı ve şifre girin.';
        return;
      }

      this.apiService.login(credentials).subscribe({
        next: () => this.router.navigate(['/map']),
        error: (err) => {
          console.error('HATA DETAYI:', err);
          this.errorMessage = 'Giriş başarısız. Kullanıcı adı veya şifre yanlış olabilir.';
        }
      });

    } else {
      // Register
      const userInfo = {
        username: this.username.trim(),
        email: this.email.trim(),
        password: this.password
      };

      if (!userInfo.username || !userInfo.email || !userInfo.password) {
        this.errorMessage = 'Lütfen tüm alanları doldurun.';
        return;
      }

      this.apiService.register(userInfo).subscribe({
        next: () => this.router.navigate(['/map']),
        error: (err) => {
          console.error('Kayıt Hatası:', err);
          this.errorMessage = 'Kayıt başarısız. Bu kullanıcı adı veya e-posta zaten kullanılıyor olabilir.';
        }
      });
    }
  }
}
