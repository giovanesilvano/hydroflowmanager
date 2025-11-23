import React, { useEffect, useState } from 'react';
import axios from 'axios';

export default function Vehicles({ token }) {
    const [list, setList] = useState([]);
    const [clients, setClients] = useState([]);
    const [form, setForm] = useState({ plate: '', type: 0, clientId: '' });
    const [editing, setEditing] = useState(null);

    useEffect(() => {
        if (token) {
            fetchVehicles();
            fetchClients();
        }
    }, [token]);

    async function fetchVehicles() {
        try {
            const res = await axios.get('http://localhost:5000/vehicles', { headers: { Authorization: 'Bearer ' + token } });
            setList(res.data);
        } catch (e) {
            alert('Erro ao buscar veículos');
        }
    }

    async function fetchClients() {
        try {
            const res = await axios.get('http://localhost:5000/clients', { headers: { Authorization: 'Bearer ' + token } });
            setClients(res.data);
        } catch (e) {
            alert('Erro ao buscar clientes');
        }
    }

    async function handleSubmit(e) {
    e.preventDefault();
    try {
        const payload = {
            plate: form.plate,
            type: parseInt(form.type),
            clientCpfCnpj: form.clientId   // 👈 nome EXATO
        };

        if (editing) {
            await axios.put(`http://localhost:5000/vehicles/${editing}`, payload, {
                headers: { Authorization: 'Bearer ' + token }
            });
            setEditing(null);
        } else {
            await axios.post('http://localhost:5000/vehicles', payload, {
                headers: { Authorization: 'Bearer ' + token }
            });
        }

        setForm({ plate: '', type: 0, clientId: '' });
        fetchVehicles();
    } catch (e) {
        console.error('Erro detalhado:', e.response?.data || e);
        alert('Erro ao salvar veículo: ' + (e.response?.data || e.message));
    }
}

    async function handleDelete(plate) {
        if (!confirm('Deseja realmente excluir este veículo?')) return;
        try {
            await axios.delete(`http://localhost:5000/vehicles/${plate}`, { headers: { Authorization: 'Bearer ' + token } });
            fetchVehicles();
        } catch (e) {
            alert('Erro ao excluir veículo');
        }
    }

    function handleEdit(vehicle) {
        setEditing(vehicle.plate);
        setForm({
            plate: vehicle.plate,
            type: vehicle.type,
            clientId: vehicle.clientId
        });
    }

    function handleCancel() {
        setEditing(null);
        setForm({ plate: '', type: 0, clientId: '' });
    }

    function getTypeName(type) {
        const types = ['Motocicleta', 'Carro Pequeno', 'Carro Grande'];
        return types[type] || 'Desconhecido';
    }

    return (
        <div>
            <h2>Veículos</h2>
            <form onSubmit={handleSubmit} className="form-card">
                <input placeholder="Placa" value={form.plate} onChange={e => setForm({ ...form, plate: e.target.value })} disabled={editing} required />
                <select value={form.type} onChange={e => setForm({ ...form, type: e.target.value })} required>
                    <option value={0}>Motocicleta</option>
                    <option value={1}>Carro Pequeno</option>
                    <option value={2}>Carro Grande</option>
                </select>
                <select value={form.clientId} onChange={e => setForm({ ...form, clientId: e.target.value })} required>
                    <option value="">Selecione o cliente</option>
                    {clients.map(c => <option key={c.cpfcnpj} value={c.cpfcnpj}>{c.name} ({c.cpfcnpj})</option>)}
                </select>
                <div className="form-actions">
                    <button type="submit">{editing ? 'Atualizar' : 'Adicionar'}</button>
                    {editing && <button type="button" onClick={handleCancel}>Cancelar</button>}
                </div>
            </form>
            <table>
                <thead>
                    <tr>
                        <th>Placa</th>
                        <th>Tipo</th>
                        <th>Cliente</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    {list.map(v => (
                        <tr key={v.plate}>
                            <td>{v.plate}</td>
                            <td>{getTypeName(v.type)}</td>
                            <td>{v.client?.name || 'N/A'}</td>
                            <td>
                                <button onClick={() => handleEdit(v)}>Editar</button>
                                <button onClick={() => handleDelete(v.plate)}>Excluir</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

