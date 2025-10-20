import React, { useEffect, useState } from 'react';
import axios from 'axios';

export default function Cash({ token }) {
    const [summary, setSummary] = useState(null);
    const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
    const [services, setServices] = useState([]);

    useEffect(() => {
        if (token) {
            fetchSummary();
            fetchServices();
        }
    }, [token, date]);

    async function fetchSummary() {
        try {
            const res = await axios.get(`http://localhost:5000/cash/summary?date=${date}`, { headers: { Authorization: 'Bearer ' + token } });
            setSummary(res.data);
        } catch (e) {
            alert('Erro ao buscar resumo do caixa');
        }
    }

    async function fetchServices() {
        try {
            const res = await axios.get('http://localhost:5000/services', { headers: { Authorization: 'Bearer ' + token } });
            setServices(res.data);
        } catch (e) {
            alert('Erro ao buscar serviços');
        }
    }

    function getServiceName(id) {
        const service = services.find(s => s.id === id);
        return service ? service.name : `Serviço #${id}`;
    }

    if (!summary) return <div>Carregando...</div>;

    return (
        <div>
            <h2>Caixa e Relatórios</h2>
            <div className="form-card">
                <label>Data:</label>
                <input type="date" value={date} onChange={e => setDate(e.target.value)} />
            </div>

            <div className="summary-cards">
                <div className="summary-card">
                    <h3>Total de Ordens</h3>
                    <p className="big-number">{summary.totalOrders}</p>
                </div>
                <div className="summary-card">
                    <h3>Receita Total</h3>
                    <p className="big-number">R$ {summary.totalReceita.toFixed(2)}</p>
                </div>
                <div className="summary-card">
                    <h3>Descontos Aplicados</h3>
                    <p className="big-number">R$ {summary.totalDescontos.toFixed(2)}</p>
                </div>
            </div>

            <h3>Por Forma de Pagamento</h3>
            <table>
                <thead>
                    <tr>
                        <th>Forma de Pagamento</th>
                        <th>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {summary.byPayment.map((p, i) => (
                        <tr key={i}>
                            <td>{p.payment}</td>
                            <td>R$ {p.total.toFixed(2)}</td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <h3>Por Serviço</h3>
            <table>
                <thead>
                    <tr>
                        <th>Serviço</th>
                        <th>Quantidade</th>
                        <th>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {summary.byService.map((s, i) => (
                        <tr key={i}>
                            <td>{getServiceName(s.serviceId)}</td>
                            <td>{s.quantity}</td>
                            <td>R$ {s.total.toFixed(2)}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

