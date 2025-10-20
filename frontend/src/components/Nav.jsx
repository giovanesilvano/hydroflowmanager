import React from 'react';

export default function Nav({ currentPage, onNavigate, onLogout }) {
    return (
        <nav className="navbar">
            <h1>HydroFlow Manager</h1>
            <div className="nav-links">
                <button className={currentPage === 'clients' ? 'active' : ''} onClick={() => onNavigate('clients')}>Clientes</button>
                <button className={currentPage === 'vehicles' ? 'active' : ''} onClick={() => onNavigate('vehicles')}>Veículos</button>
                <button className={currentPage === 'services' ? 'active' : ''} onClick={() => onNavigate('services')}>Serviços</button>
                <button className={currentPage === 'orders' ? 'active' : ''} onClick={() => onNavigate('orders')}>Ordens de Serviço</button>
                <button className={currentPage === 'cash' ? 'active' : ''} onClick={() => onNavigate('cash')}>Caixa</button>
                <button className="logout-btn" onClick={onLogout}>Sair</button>
            </div>
        </nav>
    );
}

