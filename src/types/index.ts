export interface Product {
  id: string;
  name: string;
  category: string;
  description: string;
  price?: number;
  image?: string;
  details?: string;
}

export interface Service {
  id: string;
  name: string;
  icon: string;
  description: string;
  details?: string;
}

export interface ContactForm {
  name: string;
  email: string;
  phone?: string;
  message: string;
}

export interface Message {
  id: string;
  text: string;
  sender: 'user' | 'bot';
  timestamp: Date;
}
