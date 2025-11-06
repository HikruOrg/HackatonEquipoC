import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// Angular Material
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { ApiService } from './api.service';

type Step = 'register' | 'questions' | 'summary';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressBarModule, MatChipsModule, MatListModule,
    MatFormFieldModule, MatInputModule
  ],
  templateUrl: './app.component.html'
})
export class AppComponent {
  step: Step = 'register';

  form = { email: '', firstName: '', lastName: '' };
  candidateId = '';
  questions: any[] = [];
  currentIdx = 0;

  // Recording
  media?: MediaRecorder;
  chunks: Blob[] = [];
  recording = false;
  mime = MediaRecorder.isTypeSupported('audio/webm') ? 'audio/webm'
        : (MediaRecorder.isTypeSupported('audio/mp4') ? 'audio/mp4' : 'audio/webm');

  // Submission state
  pendingFile: File | null = null;  // archivo listo para enviar (grabado o subido)
  lastResult: any = null;           // resultado de la pregunta actual
  allResults: any[] = [];           // [{ q, r }]
  loading = false;                  // cargando al llamar /analyze
  submittedForCurrent = false;      // ya se hizo submit a la pregunta actual
  errorMsg = '';
  finalLevel: string | null = null;

  constructor(private api: ApiService) {}

  get isLast() { return this.currentIdx === this.questions.length - 1; }

  async onRegister() {
    if (!this.form.email || !this.form.firstName) return;
    const r = await this.api.register(this.form).toPromise();
    this.candidateId = r!.candidateId;
    this.questions = await this.api.getQuestions().toPromise() as any[];
    this.step = 'questions';
  }

  get currentQ() { return this.questions[this.currentIdx]; }

  // --- Recording controls (no envían al BE; solo preparan archivo) ---
  async startRec() {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    this.chunks = [];
    this.media = new MediaRecorder(stream, { mimeType: this.mime });
    this.media.ondataavailable = e => { if (e.data && e.data.size > 0) this.chunks.push(e.data); };
    this.recording = true;
    this.media.start();
  }

  async stopRec() {
    if (!this.media) return;
    const media = this.media;
    this.recording = false;

    const blob: Blob = await new Promise(resolve => {
      media.onstop = () => resolve(new Blob(this.chunks, { type: this.mime }));
      media.stop();
    });

    const ext = this.mime.includes('mp4') ? 'mp4' : 'webm';
    this.pendingFile = new File([blob], `answer.${ext}`, { type: this.mime });
    this.errorMsg = '';
  }

  // Upload alternative
  async onFile(e: any) {
    const file = e.target.files?.[0];
    if (!file) return;
    this.pendingFile = file;
    this.errorMsg = '';
  }

  // --- Submit/Next flow ---
  async submitResponse() {
    if (!this.pendingFile) {
      this.errorMsg = 'Please record or upload an audio file first.';
      return;
    }
    this.loading = true;
    this.errorMsg = '';
    try {
      this.lastResult = await this.api.analyze(this.candidateId, this.currentQ.id, this.pendingFile).toPromise();
      // Guarda para el resumen final
      const existingIdx = this.allResults.findIndex(x => x.q.id === this.currentQ.id);
      if (existingIdx >= 0) this.allResults.splice(existingIdx, 1);
      this.allResults.push({ q: this.currentQ, r: this.lastResult });
      this.submittedForCurrent = true;
    } catch (err: any) {
      this.errorMsg = 'There was an error analyzing the audio. Please try again.';
    } finally {
      this.loading = false;
    }
  }

  nextQuestion() {
    if (!this.submittedForCurrent) return;
    if (this.isLast) { this.finish(); return; }
    this.currentIdx++;
    this.lastResult = null;
    this.pendingFile = null;
    this.submittedForCurrent = false;
    this.errorMsg = '';
  }

  async finish() {
    const r: any = await this.api.finish(this.candidateId).toPromise();
    const map = ['A1','A2','B1','B2','C1','C2'];
    this.finalLevel = (typeof r?.finalLevel === 'number') ? map[r.finalLevel] : r?.finalLevel ?? null;
    this.step = 'summary';
  }

  copyFollowUps() {
    const lines = this.allResults.flatMap(x => x.r.followUps.map((f: any) => `• ${f.question} — “${f.groundingQuote}”`));
    navigator.clipboard.writeText(lines.join('\n'));
  }

  // Helpers to paint rubric chips
  asBadge(v: string) {
    const s = (v || '').toLowerCase();
    return s === 'high' ? 'primary' : s === 'med' ? 'accent' : 'warn';
  }
}