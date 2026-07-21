import React from 'react';
import { Link } from 'react-router-dom';
import './Navigation.css';

const Navigation: React.FC = () => {
    return (
        <nav className="navigation">
            <ul className="nav-links">
                <li>
                    <Link to="/">Início</Link>
                </li>
                <li>
                    <Link to="/services">Serviços</Link>
                </li>
                <li>
                    <Link to="/about">Sobre</Link>
                </li>
                <li>
                    <Link to="/products">Produtos</Link>
                </li>
                <li>
                    <Link to="/contact">Contato</Link>
                </li>
            </ul>
        </nav>
    );
};

export default Navigation;
