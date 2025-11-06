import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  base = (window as any).__API__ || 'http://localhost:8080';
  constructor(private http: HttpClient) {}
  getQuestions(){ return this.http.get<any[]>(`${this.base}/questions`); }
  register(body:{email:string; firstName:string; lastName:string}) {
    return this.http.post<{candidateId:string}>(`${this.base}/register`, body);
  }
  analyze(candidateId:string, questionId:string, file:File){
    const form = new FormData();
    form.append('file', file);
    form.append('candidateId', candidateId);
    form.append('questionId', questionId);
    return this.http.post(`${this.base}/analyze`, form);
  }
  finish(candidateId:string){ return this.http.post(`${this.base}/finish`, { candidateId }); }
}