import React, { useState } from 'react';
import Login from './pages/Login';
import Clients from './pages/Clients';
import Vehicles from './pages/Vehicles';
import Services from './pages/Services';
import Orders from './pages/Orders';
import Cash from './pages/Cash';
import Nav from './components/Nav';

export default function App() {
    const [token, setToken] = useState(null);
    const [currentPage, setCurrentPage] = useState('clients');

    function handleLogout() {
        setToken(null);
        setCurrentPage('clients');
    }

    if (!token) {
        return (
            <div className='container'>
                <Login onLogin={setToken} />
            </div>
        );
    }

    return (
        <div className='container'>
            <Nav currentPage={currentPage} onNavigate={setCurrentPage} onLogout={handleLogout} />
            <div className='content'>
                {currentPage === 'clients' && <Clients token={token} />}
                {currentPage === 'vehicles' && <Vehicles token={token} />}
                {currentPage === 'services' && <Services token={token} />}
                {currentPage === 'orders' && <Orders token={token} />}
                {currentPage === 'cash' && <Cash token={token} />}
            </div>
        </div>
    );
}

