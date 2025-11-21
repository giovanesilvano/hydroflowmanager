import React, { useEffect, useState } from 'react';
import axios from 'axios';

export default function Orders({ token }) {
    const [list, setList] = useState([]);
    const [vehicles, setVehicles] = useState([]);
    const [services, setServices] = useState([]);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState({ vehiclePlate: '', paymentMethod: 0, items: [] });

    useEffect(() => {
        if (token) {
            fetchOrders();
            fetchVehicles();
            fetchServices();
        }
    }, [token]);

    async function fetchOrders() {
        try {
            const res = await axios.get('http://localhost:5000/orders', { headers: { Authorization: 'Bearer ' + token } });
            setList(res.data);
        } catch (e) {
            alert('Erro ao buscar ordens');
        }
    }

    async function fetchVehicles() {
        try {
            const res = await axios.get('http://localhost:5000/vehicles', { headers: { Authorization: 'Bearer ' + token } });
            setVehicles(res.data);
        } catch (e) {
            alert('Erro ao buscar veículos');
        }
    }

    async function fetchServices() {
        try {
            const res = await axios.get('http://localhost:5000/services', { headers: { Authorization: 'Bearer ' + token } });
            // Se o backend não retornar "active" ou retornar nulo, considerar como disponível
            setServices((res.data || []).filter(s => s.active !== false));
        } catch (e) {
            alert('Erro ao buscar serviços');
        }
    }

    async function handleSubmit(e) {
        e.preventDefault();
        if (form.items.length === 0) {
            alert('Adicione pelo menos um serviço');
            return;
        }
        try {
            const normalizedItems = (form.items || [])
                .map(it => ({
                    ServiceId: Number.parseInt(it.serviceId),
                    Quantity: Math.max(1, Number.parseInt(it.quantity))
                }))
                .filter(it => Number.isFinite(it.ServiceId) && Number.isFinite(it.Quantity) && it.ServiceId > 0);

            await axios.post('http://localhost:5000/orders', {
                VehiclePlate: form.vehiclePlate,
                PaymentMethod: Number.parseInt(form.paymentMethod),
                Items: normalizedItems,
                AttendantCPF: '00000000000'
            }, { headers: { Authorization: 'Bearer ' + token } });
            setForm({ vehiclePlate: '', paymentMethod: 0, items: [] });
            setShowForm(false);
            fetchOrders();
        } catch (e) {
            alert('Erro ao criar ordem');
        }
    }

    async function handleDelete(id) {
        if (!confirm('Deseja realmente excluir esta ordem?')) return;
        try {
            await axios.delete(`http://localhost:5000/orders/${id}`, { headers: { Authorization: 'Bearer ' + token } });
            fetchOrders();
        } catch (e) {
            alert('Erro ao excluir ordem');
        }
    }

    async function handleUpdateStatus(id, status) {
        try {
            await axios.put(`http://localhost:5000/orders/${id}/status`, { Status: Number.parseInt(status) }, {
                headers: { Authorization: 'Bearer ' + token, 'Content-Type': 'application/json' }
            });
            fetchOrders();
        } catch (e) {
            alert('Erro ao atualizar status');
        }
    }

    function addService() {
        setForm({ ...form, items: [...form.items, { serviceId: '', quantity: 1 }] });
    }

    function removeService(index) {
        const newItems = form.items.filter((_, i) => i !== index);
        setForm({ ...form, items: newItems });
    }

    function updateService(index, field, value) {
        const newItems = [...form.items];
        newItems[index][field] = field === 'quantity' ? parseInt(value) : parseInt(value);
        setForm({ ...form, items: newItems });
    }

    function getStatusName(status) {
        const statuses = ['Aberta', 'Paga', 'Cancelada'];
        return statuses[status] || 'Desconhecido';
    }

    function getPaymentName(method) {
        const methods = ['Dinheiro', 'PIX', 'Cartão Crédito', 'Cartão Débito'];
        return methods[method] || 'Desconhecido';
    }

    function formatMoney(v) {
        const n = Number.isFinite(Number(v)) ? Number(v) : 0;
        return n.toFixed(2);
    }

    return (
        <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h2>Ordens de Serviço</h2>
                <button onClick={() => setShowForm(!showForm)}>{showForm ? 'Cancelar' : 'Nova Ordem'}</button>
            </div>

            {showForm && (
                <form onSubmit={handleSubmit} className="form-card">
                    <select value={form.vehiclePlate} onChange={e => setForm({ ...form, vehiclePlate: e.target.value })} required>
                        <option value="">Selecione o veículo</option>
                        {vehicles.map(v => <option key={v.plate} value={v.plate}>{v.plate} - {v.client?.name}</option>)}
                    </select>
                    <select value={form.paymentMethod} onChange={e => setForm({ ...form, paymentMethod: e.target.value })} required>
                        <option value={0}>Dinheiro (10% desconto)</option>
                        <option value={1}>PIX</option>
                        <option value={2}>Cartão Crédito</option>
                        <option value={3}>Cartão Débito</option>
                    </select>
                    <h3>Serviços</h3>
                    {form.items.map((item, index) => (
                        <div key={index} className="service-item">
                            <select value={item.serviceId} onChange={e => updateService(index, 'serviceId', e.target.value)} required>
                                <option value="">Selecione o serviço</option>
                                {services.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                            </select>
                            <input type="number" min="1" value={item.quantity} onChange={e => updateService(index, 'quantity', e.target.value)} required />
                            <button type="button" onClick={() => removeService(index)}>Remover</button>
                        </div>
                    ))}
                    <button type="button" onClick={addService}>Adicionar Serviço</button>
                    <button type="submit">Criar Ordem</button>
                </form>
            )}

            <table>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Veículo</th>
                        <th>Cliente</th>
                        <th>Subtotal</th>
                        <th>Desconto</th>
                        <th>Total</th>
                        <th>Pagamento</th>
                        <th>Status</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    {list.map(o => (
                        <tr key={o.id || Math.random()}>
                            <td>{(o.id || '').substring(0, 8)}...</td>
                            <td>{o.vehiclePlate}</td>
                            <td>{o.vehicle?.client?.name || 'N/A'}</td>
                            <td>R$ {formatMoney(o.subtotal)}</td>
                            <td>R$ {formatMoney(o.discount)}</td>
                            <td>R$ {formatMoney(o.total)}</td>
                            <td>{getPaymentName(o.paymentMethod)}</td>
                            <td>{getStatusName(o.status)}</td>
                            <td>
                                {o.status === 0 && <button onClick={() => handleUpdateStatus(o.id, 1)}>Marcar Paga</button>}
                                {o.status === 0 && <button onClick={() => handleUpdateStatus(o.id, 2)}>Cancelar</button>}
                                <button onClick={() => handleDelete(o.id)}>Excluir</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

