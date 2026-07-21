import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import './Header.css';

const Header: React.FC = () => {
    const [open, setOpen] = useState(false);

    const menuItems = [
        { label: 'Início', to: '/' },
        { label: 'Serviços', to: '/services' },
        { label: 'Sobre', to: '/about' },
        { label: 'Produtos', to: '/products' },
        { label: 'Contato', to: '/contact' },
    ];

    const portalButton = {
        label: 'Área do Cliente',
        to: '/portal?login=true'
    };

    return (
        <header className="header">
            <div className="header-inner container">
                <div className="logo">
                    <Link to="/">
                        <img
                            src="/assets/logo.png"
                            alt="Renovo Centro Automotivo"
                            className="logo-img"
                        />
                    </Link>
                </div>

                <nav className="nav-desktop" aria-hidden={open}>
                    <ul>
                        {menuItems.map((item) => (
                            <li key={item.to}>
                                <Link to={item.to}>
                                    {item.label}
                                </Link>
                            </li>
                        ))}
                        <li key="portal" className="portal-nav-item">
                            <Link to={portalButton.to} className="portal-button">
                                {portalButton.label}
                            </Link>
                        </li>
                    </ul>
                </nav>

                <button
                    className={`hamburger ${open ? 'is-open' : ''}`}
                    aria-label="Abrir menu"
                    aria-expanded={open}
                    onClick={() => setOpen(!open)}
                >
                    <span />
                    <span />
                    <span />
                </button>

                {open && (
                    <nav className="nav-mobile" role="menu">
                        <ul>
                            {menuItems.map((item) => (
                                <li key={item.to}>
                                    <Link to={item.to} onClick={() => setOpen(false)}>
                                        <div className="mobile-nav-text">
                                            <strong>{item.label}</strong>
                                            <span>Veja mais</span>
                                        </div>
                                    </Link>
                                </li>
                            ))}
                            <li key="portal">
                                <Link to={portalButton.to} onClick={() => setOpen(false)} className="portal-mobile-link">
                                    <div className="mobile-nav-text">
                                        <strong>{portalButton.label}</strong>
                                        <span>Acesse o portal</span>
                                    </div>
                                </Link>
                            </li>
                        </ul>
                    </nav>
                )}
            </div>
        </header>
    );
};

export default Header;
