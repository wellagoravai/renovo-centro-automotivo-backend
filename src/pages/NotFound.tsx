import React from 'react';
import { Link } from 'react-router-dom';
import './NotFound.css';

const NotFound: React.FC = () => {
    return (
        <div className="not-found">
            <div className="not-found-content">
                <h1>404</h1>
                <h2>Página Não Encontrada</h2>
                <p>Desculpe, a página que você procura não existe ou foi removida.</p>
                <Link to="/" className="button">🏠 Voltar para Início</Link>
            </div>
        </div>
    );
};

export default NotFound;
