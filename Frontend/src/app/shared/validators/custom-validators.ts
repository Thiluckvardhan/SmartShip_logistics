import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Name: letters and spaces only, up to 50 chars
 * Matches: ^[A-Za-z\s]+$ and StringLength(50)
 */
export function nameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = String(control.value ?? '').trim();
    if (!value) return null; // let Required handle empty

    if (value.length < 2 || value.length > 50) {
      return { nameLength: { min: 2, max: 50, actual: value.length } };
    }
    if (!/^[A-Za-z\s]+$/.test(value)) {
      return { namePattern: true };
    }
    return null;
  };
}

/**
 * Phone: exact 10 digits
 * Used for all user-facing phone fields in frontend forms
 */
export function phoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = String(control.value ?? '').trim();
    if (!value) return null; // phone is optional in most contexts

    if (!/^\d{10}$/.test(value)) {
      return { phoneExactDigits: true };
    }
    return null;
  };
}

/**
 * Password minimum length validator
 * Login: min 6 chars (MinLength(6))
 * Register: min 6, max 100 chars (MinLength(6), MaxLength(100))
 */
export function passwordMinLengthValidator(min: number = 6): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = control.value;
    if (!value) return null; // let Required handle empty

    if (value.length < min) {
      return { passwordMinLength: { min, actual: value.length } };
    }
    return null;
  };
}

/**
 * File size: max 10MB
 * Matches: backend 10 * 1024 * 1024 check
 * Control value must be a File object
 */
export function fileSizeValidator(maxMb: number = 10): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const file: File = control.value;
    if (!file || !(file instanceof File)) return null;

    const maxBytes = maxMb * 1024 * 1024;
    if (file.size > maxBytes) {
      return { fileSize: { maxMb, actualMb: +(file.size / 1024 / 1024).toFixed(2) } };
    }
    return null;
  };
}

/**
 * File type: PDF, PNG, JPG, JPEG only
 * Matches: backend AllowedExtensions check
 * Control value must be a File object
 */
export function fileTypeValidator(allowedExts: string[] = ['.pdf', '.png', '.jpg', '.jpeg']): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const file: File = control.value;
    if (!file || !(file instanceof File)) return null;

    const fileName = file.name.toLowerCase();
    const ext = fileName.substring(fileName.lastIndexOf('.'));
    if (!allowedExts.includes(ext)) {
      return { fileType: { allowed: allowedExts, actual: ext } };
    }
    return null;
  };
}
